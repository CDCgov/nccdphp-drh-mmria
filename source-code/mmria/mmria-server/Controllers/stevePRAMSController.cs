using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.IO;
using Akka.Actor;


namespace mmria.server.Controllers;

[Authorize(Roles = "cdc_admin,steve_prams")]
public sealed class stevePRAMSController : Controller
{
    IConfiguration Configuration;

    ActorSystem _actorSystem;
    readonly ILogger<stevePRAMSController> _logger;

    string _userName = null;

    string _download_directory = null;

    Dictionary<string,string> mailbox_map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "PRAMS","PRAMS"}
    };



    public stevePRAMSController
    (
        ActorSystem actorSystem,
        ILogger<stevePRAMSController> logger,
        IConfiguration configuration
    )
    {
        _actorSystem  = actorSystem;
        _logger = logger;
        Configuration = configuration;


       

    }


    string userName
    {
        get
        {
            if (_userName == null)
            {
                if (User.Identities.Any(u => u.IsAuthenticated))
                {
                    _userName = User.Identities.First(
                        u => u.IsAuthenticated && 
                        u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
                }
            }
            return _userName;
        }
    }

    string download_directory
    {
        get
        {
            if (_download_directory == null)
            {

                _download_directory = System.IO.Path.Combine(Configuration["mmria_settings:export_directory"], userName);
            }
            return _download_directory;
        }
    }
    
    public IActionResult Index()
    {

        return View();
    }

    [HttpGet]
    public async Task<JsonResult> GetQueueResult()
    {
        var queue_Result = new mmria.common.steve.QueueResult();

        if(!System.IO.Directory.Exists(download_directory))
            return Json(queue_Result);

        var directory = new System.IO.DirectoryInfo(download_directory);
        foreach(var info in directory.GetDirectories())
        {
            if(!info.Name.StartsWith("steveMMRIA")) continue;

            if(!info.Name.Contains("PRAMS")) continue;

            var qr = new mmria.common.steve.QueueItem()
            {
                DateCreated = info.CreationTimeUtc,
                CreatedBy = userName,
                DateLastUpdated = info.LastAccessTimeUtc,
                LastUpdatedBy = userName,
                FileName = info.Name,
                ExportType = "steve",
                Status = "in-progress"
            };
            queue_Result.Items.Add(qr);
        }

        foreach(var info in directory.GetFiles())
        {
            if(!info.Name.StartsWith("steveMMRIA")) continue;

            if(!info.Name.Contains("PRAMS")) continue;

            var qr = new mmria.common.steve.QueueItem()
            {
                DateCreated = info.CreationTimeUtc,
                CreatedBy = userName,
                DateLastUpdated = info.LastAccessTimeUtc,
                LastUpdatedBy = userName,
                FileName = info.Name,
                ExportType = "steve",
                Status = "complete"
            };
            queue_Result.Items.Add(qr);
        }

        queue_Result.Items = queue_Result.Items.OrderByDescending( x=> x.DateCreated).ToList();

        return Json(queue_Result);
    }

    [HttpPost]
    public async Task<JsonResult> SetDownloadRequest
    (
        [FromBody] DownloadRequest request
    )
    {


        var queue_Result = new mmria.common.steve.QueueResult();
        if(mailbox_map.ContainsKey(request.Mailbox))
        {
            System.DateTime? result = null; 

            var processor = _actorSystem.ActorSelection("user/steve-api-supervisor");

            request.seaBucketKMSKey = Configuration["steve_api:sea_bucket_kms_key"];
            request.clientName = Configuration["steve_api:client_name"];
            request.clientSecretKey = Configuration["steve_api:client_secreat_key"];
            request.base_url = Configuration["steve_api:base_url"];

            request.download_directory = download_directory;

            request.file_name = GetFileName(request.Mailbox);

            //result = (System.DateTime) await processor.Ask(request);
            processor.Tell(request);
            
            //System.Console.WriteLine("here");

        }
        return Json(queue_Result);
    }
    

    [HttpGet]
    public  async Task<FileResult> GetFileResult(string FileName)
    {
        var queue_Result = new mmria.common.steve.QueueResult();
        var path = System.IO.Path.Combine (download_directory, FileName);

        byte[] fileBytes = GetFile(path);
        return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, FileName);

    }


    byte[] GetFile(string s)
    {
        byte[] data;
        int br;
        int fs_length;

        using
        (
            FileStream fs = new FileStream (s, FileMode.Open, FileAccess.Read)
        )
        {
            fs_length = (int) fs.Length;
            data = new byte[fs.Length];
            br = fs.Read(data, 0, data.Length);
        }
        if (br != (int) fs_length)
            throw new System.IO.IOException(s);
        return data;
    }

    string GetFileName(string p_file_name)
    {
        DateTime value = DateTime.Now;

        var year = value.Year.ToString();
        var month = value.Month.ToString().PadLeft(2,'0');
        var day = value.Day.ToString().PadLeft(2,'0');
        var hour = value.Hour.ToString().PadLeft(2,'0');
        var minute = value.Minute.ToString().PadLeft(2,'0');
        var second = value.Second.ToString().PadLeft(2,'0');
        var milli_second = value.Millisecond.ToString().PadLeft(4,'0');

        return $"steveMMRIA-{p_file_name}-{year}-{month}-{day}-{hour}-{minute}-{second}-{milli_second}";
    }

    [HttpGet]
    public  async Task<JsonResult> DeleteFileResult(string FileName)
    {
        var path = System.IO.Path.Combine (download_directory, FileName);

        if(System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return await GetQueueResult();
    }
}

