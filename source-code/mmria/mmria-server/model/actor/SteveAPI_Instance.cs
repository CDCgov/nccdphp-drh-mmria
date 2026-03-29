using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
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

            var list_mailboxes_url = BuildSteveUri(baseUri, "mailbox");
            using var mailboxRequest = new HttpRequestMessage(HttpMethod.Get, list_mailboxes_url);
            mailboxRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth_response.token);
            using var mailboxResponse = await _httpClient.SendAsync(mailboxRequest);
            mailboxResponse.EnsureSuccessStatusCode();
            response = await mailboxResponse.Content.ReadAsStringAsync();
            System.Console.WriteLine(response);

            var GetMailboxListResult = System.Text.Json.JsonSerializer.Deserialize<GetMailboxListResult>(response);            

            var download_directory = ResolveContainedPath(message.download_directory, message.file_name);

            System.IO.Directory.CreateDirectory(download_directory);

            var OneMailBoxResult = new OneMailBoxResult();

            if(message.Mailbox.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach(var item in steve_file_map.Where( x=> x.Key != "PRAMS"))
                {
                    var new_message = message with { Mailbox = item.Key };

                    var mailbox_directory = System.IO.Path.Combine(download_directory, item.Value);

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


            System.IO.File.WriteAllText
            (
                download_directory + "/download-log.txt", 
                $"STEVE Mailbox:{message.Mailbox}\nBeginDate:{ToRequestString(message.BeginDate)} => {ToBeginDateTimeRequestString(message.BeginDate)}\nEndDate:{ToRequestString(message.EndDate)} => {ToEndDateTimeRequestString(message.EndDate)}\nsuccess:{OneMailBoxResult.SuccessCount} errors:{OneMailBoxResult.ErrorList.Count} warnings:{OneMailBoxResult.WarningList.Count}\n\nErrors:\n{string.Join('\n', OneMailBoxResult.ErrorList)}\n\nWarnings:\n{string.Join('\n', OneMailBoxResult.WarningList)}"
            );


            var zip_file_name = message.file_name + ".zip";
            mmria.server.utils.cFolderCompressor folder_compressor = new mmria.server.utils.cFolderCompressor();
            string encryption_key = null;

            try
            {


                var target_zip_file = ResolveContainedPath(message.download_directory, zip_file_name);

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
        string token,
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
            using var unreadRequest = new HttpRequestMessage(HttpMethod.Get, mailbox_unread_url);
            unreadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var unreadResponse = await _httpClient.SendAsync(unreadRequest);
            unreadResponse.EnsureSuccessStatusCode();
            var response = await unreadResponse.Content.ReadAsStringAsync();

            var UnreadMessageResult = System.Text.Json.JsonSerializer.Deserialize<MailBoxMessageResult>(response);
            if(UnreadMessageResult.messages?.Length > 0)
            {

                using (var client = new System.Net.Http.HttpClient(){ Timeout = new TimeSpan(0,5, 0) })
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    foreach(var msg in UnreadMessageResult.messages)
                    {
                        var is_downloaded = false;
                        var message_id = msg.messageId;
                        var download_message_url = BuildSteveUri(baseUri, $"file/{Uri.EscapeDataString(message_id)}");
                        
                        // Sanitize filename to prevent path traversal attacks
                        var safeFileName = System.IO.Path.GetFileName(msg.fileName);
                        if (string.IsNullOrWhiteSpace(safeFileName))
                        {
                            result.ErrorList.Add($"Invalid filename from STEVE API: {msg.fileName}");
                            continue;
                        }

                        string message_path;
                        try
                        {
                            message_path = ResolveContainedPath(download_directory, safeFileName);
                        }
                        catch (ArgumentException)
                        {
                            result.ErrorList.Add($"Path traversal attempt detected in filename: {msg.fileName}");
                            continue;
                        }

                        try
                        {

                            using (var client_response = await client.GetAsync(download_message_url))
                            {
                                /*
                                using (var content = client_response.Content)
                                {
                                    using (var fs = new System.IO.FileStream(message_path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                                    {
                                        //await client_response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
                                        await client_response.Content.CopyToAsync(fs);
                                        await fs.FlushAsync();
                                        
                                    }
                                }
                                */
                                client_response.EnsureSuccessStatusCode();

                                using (System.IO.Stream contentStream = await client_response.Content.ReadAsStreamAsync(), fileStream = new System.IO.FileStream(message_path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 8192, true))
                                {
                                    const int number_of_bytes = 8192;

                                    var buffer = new byte[number_of_bytes];
                                    var isMoreToRead = true;

                                    do
                                    {
                                        var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                                        if (read == 0)
                                        {
                                            isMoreToRead = false;
                                        }
                                        else
                                        {
                                            await fileStream.WriteAsync(buffer, 0, read);
                                        }
                                    }
                                    while (isMoreToRead);
                                }
                                
                            }

                            result.SuccessCount += 1;
                            is_downloaded = true;



                        }
                        catch(Exception ex)
                        {
                            result.ErrorList.Add($"{message_path} => {ex.Message} url: {download_message_url}");
                        }
   
                        if(is_downloaded)
                        {
                            var mark_as_read_message_url = BuildSteveUri(baseUri, $"mailbox/{Uri.EscapeDataString(message_id)}");
                            try
                            {
                                using (var client_response = await client.PatchAsync(mark_as_read_message_url, null))
                                {
                                    client_response.EnsureSuccessStatusCode();
                                    var responseBody = await client_response.Content.ReadAsStringAsync();

                                    var MarkAsReadResult = System.Text.Json.JsonSerializer.Deserialize<MarkAsReadResult>(responseBody);
                                }
                            }
                            catch(Exception ex)
                            {
                                result.WarningList.Add($"Warning file downloaded, but error marking as read {message_path} => {ex.Message} url: {mark_as_read_message_url}");
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

    private static string ResolveContainedPath(string baseDirectory, string childName)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentException("Base directory is required.", nameof(baseDirectory));
        }

        var safeChildName = System.IO.Path.GetFileName(childName);
        if (string.IsNullOrWhiteSpace(safeChildName))
        {
            throw new ArgumentException("Child path must resolve to a file or directory name.", nameof(childName));
        }

        var rootPath = System.IO.Path.GetFullPath(baseDirectory);
        var combinedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootPath, safeChildName));
        if (!combinedPath.StartsWith(rootPath + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !combinedPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Resolved path escaped the configured base directory.", nameof(childName));
        }

        return combinedPath;
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


