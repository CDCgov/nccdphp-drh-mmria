using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Akka.Actor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mmria.common.steve;
using mmria.server.util;

namespace mmria.server;

public sealed class SteveAPI_Instance : ReceiveActor
{

    public record class Status(string Name, string Description);

    Dictionary<string,string> steve_file_map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Mortality","Mortality"},
        { "Fetal Death","FetalDeath"},
        { "Natality", "Natality"},
        { "Other", "Other"},
        { "PRAMS", "Natality"},

    };


    IConfiguration configuration;
    ILogger logger;
    private readonly HttpClient _httpClient;
    private readonly HttpClient _downloadHttpClient;
    private static readonly Regex SteveBearerTokenPattern = new("^[A-Za-z0-9._~+/=-]{1,4096}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    
    public SteveAPI_Instance()
    {
        var factory = new mmria.common.SimpleHttpClientFactory();
        _httpClient = factory.CreateClient(string.Empty);
        _downloadHttpClient = factory.CreateClient(string.Empty);
        _downloadHttpClient.Timeout = TimeSpan.FromMinutes(5);
        ReceiveAsync<DownloadRequest>(async message =>
        {
            var AuthRequestBody = new AuthRequestBody()
            {
                seaBucketKMSKey = message.seaBucketKMSKey,
                clientName = message.clientName,
                clientSecretKey = message.clientSecretKey
            };

            var baseUri = ValidateSteveBaseUri(message.base_url);
            var auth_url = BuildSteveUri(baseUri, "auth");

            // Serialize to stream to avoid keeping secrets in heap as string
            using var ms = new System.IO.MemoryStream();
            await System.Text.Json.JsonSerializer.SerializeAsync(ms, AuthRequestBody);
            ms.Position = 0;
            
            using var authContent = new StreamContent(ms);
            authContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            using var authResponse = await _httpClient.PostAsync(auth_url, authContent);
            authResponse.EnsureSuccessStatusCode();
            var response = await authResponse.Content.ReadAsStringAsync();

            //System.Console.WriteLine(response);

            var auth_response = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(response);

            if (auth_response == null || string.IsNullOrWhiteSpace(auth_response.token))
            {
                throw new InvalidOperationException("STEVE authentication response did not include a bearer token.");
            }

            var list_mailboxes_url = BuildSteveUri(baseUri, "mailbox");
            using var mailboxRequest = CreateSteveRequest(HttpMethod.Get, list_mailboxes_url, auth_response.token);
            using var mailboxResponse = await _httpClient.SendAsync(mailboxRequest);
            mailboxResponse.EnsureSuccessStatusCode();
            response = await mailboxResponse.Content.ReadAsStringAsync();

            var GetMailboxListResult = System.Text.Json.JsonSerializer.Deserialize<GetMailboxListResult>(response);
            if (GetMailboxListResult?.mailboxes == null)
            {
                throw new InvalidOperationException("STEVE mailbox list response did not include any mailboxes.");
            }

            var downloadRootDirectory = ContainedPathHelper.NormalizeTrustedDirectoryRoot(message.download_directory, nameof(message.download_directory));
            var downloadDirectoryName = ContainedPathHelper.ValidateContainedName(message.file_name, nameof(message.file_name));
            var download_directory = ContainedPathHelper.ResolveContainedDirectoryPath(downloadRootDirectory, downloadDirectoryName);

            System.IO.Directory.CreateDirectory(download_directory);

            var OneMailBoxResult = new OneMailBoxResult();

            if(message.Mailbox.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach(var item in steve_file_map.Where( x=> x.Key != "PRAMS"))
                {
                    var new_message = message with { Mailbox = item.Key };

                    var mailbox_directory = ContainedPathHelper.ResolveContainedDirectoryPath(download_directory, item.Value);

                    System.IO.Directory.CreateDirectory(mailbox_directory);
                    var one_mailbox_response = await OneMailBox
                    (
                        new_message,
                        GetMailboxListResult,
                        baseUri,
                        auth_response.token,
                        mailbox_directory
                    );

                    OneMailBoxResult.SuccessCount += one_mailbox_response.SuccessCount;
                    OneMailBoxResult.ErrorList.AddRange(one_mailbox_response.ErrorList);

                }
            }
            else
            {
                var one_mailbox_response = await OneMailBox
                (
                    message,
                    GetMailboxListResult,
                    baseUri,
                    auth_response.token,
                    download_directory
                );

                OneMailBoxResult.SuccessCount += one_mailbox_response.SuccessCount;
                OneMailBoxResult.ErrorList.AddRange(one_mailbox_response.ErrorList);
            }

            var downloadLogPath = ContainedPathHelper.ResolveContainedFilePath(download_directory, "download-log.txt");
            System.IO.File.WriteAllText
            (
                downloadLogPath,
                $"STEVE Mailbox:{message.Mailbox}\nBeginDate:{ToRequestString(message.BeginDate)} => {ToBeginDateTimeRequestString(message.BeginDate)}\nEndDate:{ToRequestString(message.EndDate)} => {ToEndDateTimeRequestString(message.EndDate)}\nsuccess:{OneMailBoxResult.SuccessCount} errors:{OneMailBoxResult.ErrorList.Count} warnings:{OneMailBoxResult.WarningList.Count}\n\nErrors:\n{string.Join('\n', OneMailBoxResult.ErrorList)}\n\nWarnings:\n{string.Join('\n', OneMailBoxResult.WarningList)}"
            );


            var zip_file_name = ContainedPathHelper.ValidateContainedName(message.file_name + ".zip", nameof(message.file_name));
            mmria.server.utils.cFolderCompressor folder_compressor = new mmria.server.utils.cFolderCompressor();
            string encryption_key = null;

            try
            {

                var target_zip_file = ContainedPathHelper.ResolveContainedFilePath(downloadRootDirectory, zip_file_name);

                if(System.IO.File.Exists(target_zip_file))
                {
                    System.IO.File.Delete(target_zip_file);
                }

                if(System.IO.Directory.Exists(download_directory))
                {
                    folder_compressor.Compress
                    (
                        target_zip_file,
                        encryption_key,
                        download_directory
                    );

                    System.IO.Directory.Delete(download_directory, true);
                }

            }
            catch(Exception)
            {
                Console.WriteLine("STEVE file compression failed.");
            }
                    

            System.Console.WriteLine("here");

            Context.Stop(this.Self);
        });
    }

    async Task<OneMailBoxResult> OneMailBox
    (
        DownloadRequest message,
        GetMailboxListResult GetMailboxListResult,
        Uri baseUri,
        string bearerToken,
        string download_directory
    )
    {
        var result = new OneMailBoxResult();
        foreach(var mail_box in GetMailboxListResult.mailboxes)
        {
            if(mail_box.routingCode != "DRH") continue;

            if(!steve_file_map.ContainsKey(message.Mailbox)) continue;
            
            if
            (
                message.Mailbox == "PRAMS" 
            )
            {
                if
                (
                    mail_box.listName.ToUpper() != "PRAMS"
                )   
                continue;
            }
            else if
            (
                mail_box.listName.ToUpper() != "JURISDICTION DATA" ||  
                mail_box.fileType != steve_file_map[message.Mailbox]
            ) continue;

            

            var mailbox_unread_url = BuildSteveUri(
                baseUri,
                $"mailbox/{Uri.EscapeDataString(mail_box.mailboxId)}/all",
                $"count=1000&fromDate={Uri.EscapeDataString(ToBeginDateTimeRequestString(message.BeginDate))}&toDate={Uri.EscapeDataString(ToEndDateTimeRequestString(message.EndDate))}");
            using var unreadRequest = CreateSteveRequest(HttpMethod.Get, mailbox_unread_url, bearerToken);
            using var unreadResponse = await _httpClient.SendAsync(unreadRequest);
            unreadResponse.EnsureSuccessStatusCode();
            var response = await unreadResponse.Content.ReadAsStringAsync();

            var UnreadMessageResult = System.Text.Json.JsonSerializer.Deserialize<MailBoxMessageResult>(response);
            if(UnreadMessageResult.messages?.Length > 0)
            {
                foreach(var msg in UnreadMessageResult.messages)
                {
                    var is_downloaded = false;
                    var message_id = msg.messageId;
                    var download_message_url = BuildSteveUri(baseUri, $"file/{Uri.EscapeDataString(message_id)}");
                    string safeFileLabel = SanitizeDisplayValue(msg.fileName);
                    var safeDownloadFileName = ContainedPathHelper.CreateSafeContainedName(
                        msg.fileName,
                        $"steve-{message_id}");

                    try
                    {
                        using var downloadRequest = CreateSteveRequest(HttpMethod.Get, download_message_url, bearerToken);
                        using (var client_response = await _downloadHttpClient.SendAsync(downloadRequest, HttpCompletionOption.ResponseHeadersRead))
                        {
                            client_response.EnsureSuccessStatusCode();

                            using (System.IO.Stream contentStream = await client_response.Content.ReadAsStreamAsync())
                            {
                                await using var fileStream = ContainedPathHelper.OpenContainedWriteStream(download_directory, safeDownloadFileName);
                                await contentStream.CopyToAsync(fileStream);
                                await fileStream.FlushAsync();
                            }
                        }

                        result.SuccessCount += 1;
                        is_downloaded = true;

                    }
                    catch(Exception)
                    {
                        Console.WriteLine("STEVE file download failed.");
                        result.ErrorList.Add($"Failed to download STEVE file '{safeFileLabel}'.");
                    }

                    if(is_downloaded)
                    {
                        var mark_as_read_message_url = BuildSteveUri(baseUri, $"mailbox/{Uri.EscapeDataString(message_id)}");
                        try
                        {
                            using var markAsReadRequest = CreateSteveRequest(HttpMethod.Patch, mark_as_read_message_url, bearerToken);
                            using (var client_response = await _httpClient.SendAsync(markAsReadRequest))
                            {
                                client_response.EnsureSuccessStatusCode();
                                var responseBody = await client_response.Content.ReadAsStringAsync();

                                var MarkAsReadResult = System.Text.Json.JsonSerializer.Deserialize<MarkAsReadResult>(responseBody);
                            }
                        }
                        catch(Exception)
                        {
                            Console.WriteLine("STEVE mark-as-read request failed.");
                            result.WarningList.Add($"Downloaded STEVE file '{safeFileLabel}', but marking it as read failed.");
                        }
                    }
                }
            }
        }
        return result;
    }

    private static Uri ValidateSteveBaseUri(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedUri))
        {
            throw new ArgumentException("STEVE base URL must be an absolute URI.", nameof(baseUrl));
        }

        if (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("STEVE base URL must use HTTP or HTTPS.", nameof(baseUrl));
        }

        if (!string.IsNullOrWhiteSpace(parsedUri.UserInfo) || !string.IsNullOrWhiteSpace(parsedUri.Fragment))
        {
            throw new ArgumentException("STEVE base URL must not contain user info or fragments.", nameof(baseUrl));
        }

        var normalizedPath = parsedUri.AbsolutePath.TrimEnd('/');
        var normalizedUri = new UriBuilder(parsedUri)
        {
            Path = string.IsNullOrWhiteSpace(normalizedPath) ? "/" : normalizedPath + "/",
            Query = string.Empty,
            Fragment = string.Empty
        };

        return normalizedUri.Uri;
    }

    internal static HttpRequestMessage CreateSteveRequest(HttpMethod method, Uri requestUri, string bearerToken)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = CreateSteveAuthenticationHeaderValue(bearerToken);
        return request;
    }

    private static Uri BuildSteveUri(Uri baseUri, string relativePath, string query = null)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("STEVE relative path is required.", nameof(relativePath));
        }

        var targetUri = new Uri(baseUri, relativePath.TrimStart('/'));
        if (!Uri.Compare(baseUri, targetUri, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase).Equals(0))
        {
            throw new ArgumentException("Derived STEVE URI escaped the configured host.");
        }

        if (!targetUri.AbsolutePath.StartsWith(baseUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Derived STEVE URI escaped the configured base path.");
        }

        if (query == null)
        {
            return targetUri;
        }

        return new UriBuilder(targetUri)
        {
            Query = query
        }.Uri;
    }

    private static AuthenticationHeaderValue CreateSteveAuthenticationHeaderValue(string bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            throw new ArgumentException("STEVE bearer token is required.", nameof(bearerToken));
        }

        var trimmedToken = bearerToken.Trim();
        if (!SteveBearerTokenPattern.IsMatch(trimmedToken))
        {
            throw new ArgumentException("STEVE bearer token contains unexpected characters.", nameof(bearerToken));
        }

        return new AuthenticationHeaderValue("Bearer", trimmedToken);
    }

    private static string SanitizeDisplayValue(string value, int maxLength = 120)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var singleLineValue = new string(value.Where(character => !char.IsControl(character)).ToArray())
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        while (singleLineValue.Contains("  ", StringComparison.Ordinal))
        {
            singleLineValue = singleLineValue.Replace("  ", " ", StringComparison.Ordinal);
        }

        return singleLineValue.Length > maxLength
            ? singleLineValue[..maxLength]
            : singleLineValue;
    }

    class OneMailBoxResult
    {
        public OneMailBoxResult(){}
        public int SuccessCount {get;set;}
        public List<string> ErrorList {get;set;} = new();
        public List<string> WarningList {get;set;} = new();
    }
    string ToRequestString(DateTime value)
    {
        var year = value.Year.ToString();
        var month = value.Month.ToString().PadLeft(2,'0');
        var day = value.Day.ToString().PadLeft(2,'0');

        return $"{year}-{month}-{day}";
    }

    string ToBeginDateTimeRequestString(DateTime value)
    {
        /*
        var yesterday = value.AddDays(- 1);

        var year = yesterday.Year.ToString();
        var month = yesterday.Month.ToString().PadLeft(2,'0');
        var day = yesterday.Day.ToString().PadLeft(2,'0');

        return $"{year}-{month}-{day}T19:00:00Z";
        */

        var year = value.Year.ToString();
        var month = value.Month.ToString().PadLeft(2,'0');
        var day = value.Day.ToString().PadLeft(2,'0');

        return $"{year}-{month}-{day}";
    }

    string ToEndDateTimeRequestString(DateTime value)
    {
        /*
        var year = value.Year.ToString();
        var month = value.Month.ToString().PadLeft(2,'0');
        var day = value.Day.ToString().PadLeft(2,'0');

        return $"{year}-{month}-{day}T18:59:00Z";
        */
        var year = value.Year.ToString();
        var month = value.Month.ToString().PadLeft(2,'0');
        var day = value.Day.ToString().PadLeft(2,'0');

        return $"{year}-{month}-{day}";
    }



}


