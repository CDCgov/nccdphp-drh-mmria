using System;
using System.Net.Http;
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

            var base_url = message.base_url;
            var auth_url = $"{base_url}/auth";

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

            var list_mailboxes_url = $"{base_url}/mailbox";
            using var mailboxRequest = new HttpRequestMessage(HttpMethod.Get, list_mailboxes_url);
            mailboxRequest.Headers.Add("Authorization", "Bearer " + auth_response.token);
            using var mailboxResponse = await _httpClient.SendAsync(mailboxRequest);
            mailboxResponse.EnsureSuccessStatusCode();
            response = await mailboxResponse.Content.ReadAsStringAsync();
            System.Console.WriteLine(response);

            var GetMailboxListResult = System.Text.Json.JsonSerializer.Deserialize<GetMailboxListResult>(response);            

            var download_directory = System.IO.Path.Combine(message.download_directory,message.file_name);

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
                        base_url,
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
                    base_url,
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


                var target_zip_file = System.IO.Path.Combine(message.download_directory, zip_file_name);

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
        string base_url,
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

            

            var mailbox_unread_url = $"{base_url}/mailbox/{mail_box.mailboxId}/all?count=1000&fromDate={ToBeginDateTimeRequestString(message.BeginDate)}&toDate={ToEndDateTimeRequestString(message.EndDate)}";
            using var unreadRequest = new HttpRequestMessage(HttpMethod.Get, mailbox_unread_url);
            unreadRequest.Headers.Add("Authorization", "Bearer " + token);
            using var unreadResponse = await _httpClient.SendAsync(unreadRequest);
            unreadResponse.EnsureSuccessStatusCode();
            var response = await unreadResponse.Content.ReadAsStringAsync();

            var UnreadMessageResult = System.Text.Json.JsonSerializer.Deserialize<MailBoxMessageResult>(response);
            if(UnreadMessageResult.messages?.Length > 0)
            {

                using (var client = new System.Net.Http.HttpClient(){ Timeout = new TimeSpan(0,5, 0) })
                {
                    client.DefaultRequestHeaders.Add("Authorization","Bearer " + token);

                    foreach(var msg in UnreadMessageResult.messages)
                    {
                        var is_downloaded = false;
                        var message_id = msg.messageId;
                        var download_message_url = $"{base_url}/file/{message_id}";
                        
                        // Sanitize filename to prevent path traversal attacks
                        var safeFileName = System.IO.Path.GetFileName(msg.fileName);
                        if (string.IsNullOrWhiteSpace(safeFileName))
                        {
                            result.ErrorList.Add($"Invalid filename from STEVE API: {msg.fileName}");
                            continue;
                        }

                        var message_path = System.IO.Path.Combine(download_directory, safeFileName);
                        var fullPath = System.IO.Path.GetFullPath(message_path);
                        var allowedDirectory = System.IO.Path.GetFullPath(download_directory);

                        // Verify the resolved path is within the expected directory
                        if (!fullPath.StartsWith(allowedDirectory + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) 
                            && !fullPath.Equals(allowedDirectory, StringComparison.OrdinalIgnoreCase))
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
                            var mark_as_read_message_url = $"{base_url}/mailbox/{message_id}";
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


