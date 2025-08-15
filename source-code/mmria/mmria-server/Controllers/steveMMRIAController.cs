using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Akka.Actor;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server.Controllers;

[Authorize(Roles = "cdc_admin,steve_mmria")]
public sealed class steveMMRIAController : Controller
{



    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    ActorSystem _actorSystem;

    readonly ILogger<steveMMRIAController> _logger;

    string _userName = null;
    string _download_directory = null;
    Dictionary<string,string> mailbox_map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "All", "All"},
        { "Mortality","Mortality"},
        { "Fetal Death","FetalDeath"},
        { "Natality", "Natality"},
        { "Other", "Other"}
        
    };

    public steveMMRIAController
    (
        ActorSystem actorSystem,
        ILogger<steveMMRIAController> logger,
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        _actorSystem = actorSystem;
        _logger = logger;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    string userName
    {
        get
        {
            if (_userName == null)
            {
                if (User.Identities.Any(u => u.IsAuthenticated))
                {
                    _userName = User.Identities.First
                    (
                        u => u.IsAuthenticated && 
                        u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)
                    )
                    .FindFirst(System.Security.Claims.ClaimTypes.Name)
                    .Value.Replace("@","-").Replace("'","-");
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

                _download_directory = System.IO.Path.Combine(configuration.GetString("export_directory", host_prefix), userName);
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
            
            if(info.Name.Contains("PRAMS")) continue;

            var qr = new mmria.common.steve.QueueItem()
            {
                DateCreated = info.CreationTimeUtc,
                CreatedBy = userName,
                DateLastUpdated = info.LastAccessTimeUtc,
                LastUpdatedBy = userName,
                FileName = info.Name,
                ExportType = "steve",
                Status = "In Progress"
            };

            var time_diff = DateTime.Now - qr.DateCreated;
            if(time_diff.Hours > 1)
            {
                qr.Status = "Cancelled";
            }
            queue_Result.Items.Add(qr);
        }

        foreach(var info in directory.GetFiles())
        {
            if(!info.Name.StartsWith("steveMMRIA")) continue;
            
            if(info.Name.Contains("PRAMS")) continue;

            var qr = new mmria.common.steve.QueueItem()
            {
                DateCreated = info.CreationTimeUtc,
                CreatedBy = userName,
                DateLastUpdated = info.LastAccessTimeUtc,
                LastUpdatedBy = userName,
                FileName = info.Name,
                ExportType = "steve",
                Status = "Complete"
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

            var steve_api = configuration.GetSteveAPIConfigurationDetail();

            request.seaBucketKMSKey = steve_api.sea_bucket_kms_key;
            request.clientName = steve_api.client_name;
            request.clientSecretKey = steve_api.client_secret_key;
            request.base_url = steve_api.base_url;
            request.download_directory = download_directory;
            request.file_name = GetFileName(request.Mailbox);

            var processor = _actorSystem.ActorSelection("user/steve-api-supervisor");

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


    byte[] GetFile(string s)
    {
        byte[] data;
        int br;
        int fs_length;

        using(FileStream fs = new FileStream (s, FileMode.Open, FileAccess.Read))
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

        return $"steveMMRIA-{p_file_name}-{year}-{month}-{day}T{hour}-{minute}-{second}-{milli_second}";
    }


}

