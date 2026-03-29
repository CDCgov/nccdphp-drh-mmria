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
    private static readonly Regex SteveBearerTokenPattern = new("^[A-Za-z0-9._~+/=-]{1,4096}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    protected override void PreStart() => Console.WriteLine("Process_Message started");
    protected override void PostStop() => Console.WriteLine("Process_Message stopped");
    
    public SteveAPI_Instance()
    {
        var factory = new mmria.common.SimpleHttpClientFactory();
        _httpClient = factory.CreateClient(string.Empty);
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

            var bearerAuthorization = SteveAuthorizationHeader.Create(auth_response.token);

            var list_mailboxes_url = BuildSteveUri(baseUri, "mailbox");
            using var mailboxRequest = CreateSteveRequest(HttpMethod.Get, list_mailboxes_url, bearerAuthorization);
            using var mailboxResponse = await _httpClient.SendAsync(mailboxRequest);
            mailboxResponse.EnsureSuccessStatusCode();
            response = await mailboxResponse.Content.ReadAsStringAsync();
            System.Console.WriteLine(response);

            var GetMailboxListResult = System.Text.Json.JsonSerializer.Deserialize<GetMailboxListResult>(response);
            var downloadRootDirectory = NormalizeTrustedDirectoryRoot(message.download_directory, nameof(message.download_directory));
            var downloadDirectoryName = ValidateContainedName(message.file_name, nameof(message.file_name));
            var download_directory = ResolveContainedDirectoryPath(downloadRootDirectory, downloadDirectoryName);

            System.IO.Directory.CreateDirectory(download_directory);

            var OneMailBoxResult = new OneMailBoxResult();

            if(message.Mailbox.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach(var item in steve_file_map.Where( x=> x.Key != "PRAMS"))
                {
                    var new_message = message with { Mailbox = item.Key };

                    var mailbox_directory = ResolveContainedDirectoryPath(download_directory, item.Value);

                    System.IO.Directory.CreateDirectory(mailbox_directory);
                    var one_mailbox_response = await OneMailBox
                    (
                        new_message,
                        GetMailboxListResult,
                        baseUri,
                        bearerAuthorization,
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
                    bearerAuthorization,
                    download_directory
                );

                OneMailBoxResult.SuccessCount += one_mailbox_response.SuccessCount;
                OneMailBoxResult.ErrorList.AddRange(one_mailbox_response.ErrorList);
            }

            var downloadLogPath = ResolveContainedFilePath(download_directory, "download-log.txt");
            System.IO.File.WriteAllText
            (
                downloadLogPath,
                $"STEVE Mailbox:{message.Mailbox}\nBeginDate:{ToRequestString(message.BeginDate)} => {ToBeginDateTimeRequestString(message.BeginDate)}\nEndDate:{ToRequestString(message.EndDate)} => {ToEndDateTimeRequestString(message.EndDate)}\nsuccess:{OneMailBoxResult.SuccessCount} errors:{OneMailBoxResult.ErrorList.Count} warnings:{OneMailBoxResult.WarningList.Count}\n\nErrors:\n{string.Join('\n', OneMailBoxResult.ErrorList)}\n\nWarnings:\n{string.Join('\n', OneMailBoxResult.WarningList)}"
            );


            var zip_file_name = ValidateContainedName(message.file_name + ".zip", nameof(message.file_name));
            mmria.server.utils.cFolderCompressor folder_compressor = new mmria.server.utils.cFolderCompressor();
            string encryption_key = null;

            try
            {

                var target_zip_file = ResolveContainedFilePath(downloadRootDirectory, zip_file_name);

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
            catch(Exception ex)
            {
                Console.WriteLine($"File Compressor \n{ex}");
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
        SteveAuthorizationHeader bearerAuthorization,
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
            using var unreadRequest = CreateSteveRequest(HttpMethod.Get, mailbox_unread_url, bearerAuthorization);
            using var unreadResponse = await _httpClient.SendAsync(unreadRequest);
            unreadResponse.EnsureSuccessStatusCode();
            var response = await unreadResponse.Content.ReadAsStringAsync();

            var UnreadMessageResult = System.Text.Json.JsonSerializer.Deserialize<MailBoxMessageResult>(response);
            if(UnreadMessageResult.messages?.Length > 0)
            {
                using (var client = new System.Net.Http.HttpClient(){ Timeout = new TimeSpan(0,5, 0) })
                {
                    foreach(var msg in UnreadMessageResult.messages)
                    {
                        var is_downloaded = false;
                        var message_id = msg.messageId;
                        var download_message_url = BuildSteveUri(baseUri, $"file/{Uri.EscapeDataString(message_id)}");
                        SteveContainedFile messageTarget;
                        try
                        {
                            messageTarget = SteveContainedFile.Create(download_directory, msg.fileName);
                        }
                        catch (ArgumentException)
                        {
                            result.ErrorList.Add($"Path traversal attempt detected in filename: {msg.fileName}");
                            continue;
                        }

                        try
                        {
                            using var downloadRequest = CreateSteveRequest(HttpMethod.Get, download_message_url, bearerAuthorization);
                            using (var client_response = await client.SendAsync(downloadRequest, HttpCompletionOption.ResponseHeadersRead))
                            {
                                client_response.EnsureSuccessStatusCode();

                                using (System.IO.Stream contentStream = await client_response.Content.ReadAsStreamAsync())
                                {
                                    await using var fileStream = messageTarget.OpenWriteStream();
                                    await contentStream.CopyToAsync(fileStream);
                                    await fileStream.FlushAsync();
                                }
                                
                            }

                            result.SuccessCount += 1;
                            is_downloaded = true;



                        }
                        catch(Exception ex)
                        {
                            result.ErrorList.Add($"{messageTarget.FullPath} => {ex.Message} url: {download_message_url}");
                        }
    
                        if(is_downloaded)
                        {
                            var mark_as_read_message_url = BuildSteveUri(baseUri, $"mailbox/{Uri.EscapeDataString(message_id)}");
                            try
                            {
                                using var markAsReadRequest = CreateSteveRequest(HttpMethod.Patch, mark_as_read_message_url, bearerAuthorization);
                                using (var client_response = await client.SendAsync(markAsReadRequest))
                                {
                                    client_response.EnsureSuccessStatusCode();
                                    var responseBody = await client_response.Content.ReadAsStringAsync();

                                    var MarkAsReadResult = System.Text.Json.JsonSerializer.Deserialize<MarkAsReadResult>(responseBody);
                                }
                            }
                            catch(Exception ex)
                            {
                                result.WarningList.Add($"Warning file downloaded, but error marking as read {messageTarget.FullPath} => {ex.Message} url: {mark_as_read_message_url}");
                            }
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

    private static HttpRequestMessage CreateSteveRequest(HttpMethod method, Uri requestUri, SteveAuthorizationHeader bearerAuthorization)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = bearerAuthorization.Value;
        return request;
    }

    private static Uri BuildSteveUri(Uri baseUri, string relativePath, string query = null)
    {
        var targetUri = new Uri(baseUri, relativePath.TrimStart('/'));
        if (!Uri.Compare(baseUri, targetUri, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase).Equals(0))
        {
            throw new ArgumentException("Derived STEVE URI escaped the configured host.");
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

    private static string NormalizeTrustedDirectoryRoot(string baseDirectory, string paramName)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentException("Base directory is required.", paramName);
        }

        var rootPath = System.IO.Path.GetFullPath(baseDirectory);
        if (!System.IO.Path.IsPathFullyQualified(rootPath))
        {
            throw new ArgumentException("Base directory must be fully qualified.", paramName);
        }

        return System.IO.Path.EndsInDirectorySeparator(rootPath)
            ? rootPath
            : rootPath + System.IO.Path.DirectorySeparatorChar;
    }

    private static string ResolveContainedDirectoryPath(string trustedBaseDirectory, string childDirectoryName)
    {
        var normalizedRoot = NormalizeTrustedDirectoryRoot(trustedBaseDirectory, nameof(trustedBaseDirectory));
        var safeDirectoryName = ValidateContainedName(childDirectoryName, nameof(childDirectoryName));
        var combinedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(normalizedRoot, safeDirectoryName));
        EnsureContainedPath(normalizedRoot, combinedPath, nameof(childDirectoryName));
        return combinedPath;
    }

    private static string ResolveContainedFilePath(string trustedBaseDirectory, string fileName)
    {
        var normalizedRoot = NormalizeTrustedDirectoryRoot(trustedBaseDirectory, nameof(trustedBaseDirectory));
        var safeFileName = ValidateContainedName(fileName, nameof(fileName));
        var combinedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(normalizedRoot, safeFileName));
        EnsureContainedPath(normalizedRoot, combinedPath, nameof(fileName));
        return combinedPath;
    }

    private static string ValidateContainedName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty path segment is required.", paramName);
        }

        var trimmedValue = value.Trim();
        if (trimmedValue is "." or "..")
        {
            throw new ArgumentException("Relative path operators are not allowed.", paramName);
        }

        if (System.IO.Path.IsPathRooted(trimmedValue) ||
            trimmedValue.Contains(System.IO.Path.DirectorySeparatorChar) ||
            trimmedValue.Contains(System.IO.Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("Only a single file or directory name is allowed.", paramName);
        }

        if (trimmedValue.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Path segment contains invalid filename characters.", paramName);
        }

        return trimmedValue;
    }

    private static void EnsureContainedPath(string trustedBaseDirectory, string resolvedPath, string paramName)
    {
        if (!resolvedPath.StartsWith(trustedBaseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Resolved path escaped the configured base directory.", paramName);
        }
    }

    private sealed class SteveAuthorizationHeader
    {
        private SteveAuthorizationHeader(AuthenticationHeaderValue value)
        {
            Value = value;
        }

        public AuthenticationHeaderValue Value { get; }

        public static SteveAuthorizationHeader Create(string bearerToken)
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

            return new SteveAuthorizationHeader(new AuthenticationHeaderValue("Bearer", trimmedToken));
        }
    }

    private sealed class SteveContainedFile
    {
        private SteveContainedFile(string fullPath)
        {
            FullPath = fullPath;
        }

        public string FullPath { get; }

        public static SteveContainedFile Create(string trustedBaseDirectory, string fileName)
        {
            var normalizedRoot = NormalizeTrustedDirectoryRoot(trustedBaseDirectory, nameof(trustedBaseDirectory));
            var safeFileName = ValidateContainedName(fileName, nameof(fileName));
            var combinedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(normalizedRoot, safeFileName));
            EnsureContainedPath(normalizedRoot, combinedPath, nameof(fileName));
            return new SteveContainedFile(combinedPath);
        }

        public System.IO.FileStream OpenWriteStream() =>
            new(FullPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 8192, true);
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


