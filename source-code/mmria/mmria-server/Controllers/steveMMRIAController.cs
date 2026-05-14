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
using mmria.server.util;
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
        mmria.server.util.RequestTenantRuntime tenantRuntime
    )
    {
        _actorSystem = actorSystem;
        _logger = logger;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
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
                    .Value;
                }

                _userName = ContainedPathHelper.CreateSafeContainedName(_userName, "user");
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
                _download_directory = ContainedPathHelper.ResolveContainedDirectoryPath(
                    configuration.GetString("export_directory", host_prefix),
                    userName);
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
    public async Task<JsonResult> SetDownloadRequest()
    {
        var queue_Result = new mmria.common.steve.QueueResult();
        var request = await JsonRequestBodyReader.ReadAsync<DownloadRequestBody>(Request);
        var inboundRequest = CreateSanitizedInboundRequest(request);

        if(inboundRequest != null && mailbox_map.ContainsKey(inboundRequest.Mailbox))
        {
            System.DateTime? result = null; 

            var steve_api = configuration.GetSteveAPIConfigurationDetail();
            var safeRequest = new DownloadRequest
            {
                BeginDate = inboundRequest.BeginDate,
                EndDate = inboundRequest.EndDate,
                Mailbox = inboundRequest.Mailbox,
                seaBucketKMSKey = steve_api.sea_bucket_kms_key,
                clientName = steve_api.client_name,
                clientSecretKey = steve_api.client_secret_key,
                base_url = steve_api.base_url,
                download_directory = download_directory,
                file_name = GetFileName(inboundRequest.Mailbox)
            };

            var processor = _actorSystem.ActorSelection("user/steve-api-supervisor");

            //result = (System.DateTime) await processor.Ask(request);
            processor.Tell(safeRequest);
            
            //System.Console.WriteLine("here");

   

        }

        
        return Json(queue_Result);
    }

    public sealed class DownloadRequestBody
    {
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Mailbox { get; set; }
    }

    private static DownloadRequest CreateSanitizedInboundRequest(DownloadRequestBody request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Mailbox))
        {
            return null;
        }

        return new DownloadRequest
        {
            BeginDate = request.BeginDate,
            EndDate = request.EndDate,
            Mailbox = request.Mailbox.Trim()
        };
    }
    

    [HttpGet]
    public  async Task<IActionResult> GetFileResult(string FileName)
    {
        string safeFileName;
        try
        {
            safeFileName = ContainedPathHelper.ValidateContainedName(FileName, nameof(FileName));
        }
        catch (ArgumentException)
        {
            return NotFound();
        }

        if (!ContainedPathHelper.TryFindExistingFile(download_directory, safeFileName, out var fileInfo))
        {
            return NotFound();
        }

        byte[] fileBytes = await ContainedPathHelper.ReadExistingFileAsync(fileInfo);
        return SafeFileDownloadResultFactory.Create(
            fileBytes,
            System.Net.Mime.MediaTypeNames.Application.Octet,
            safeFileName,
            "steve-download.bin");

    }

    [HttpGet]
    public  async Task<JsonResult> DeleteFileResult(string FileName)
    {
        try
        {
            var safeFileName = ContainedPathHelper.ValidateContainedName(FileName, nameof(FileName));
            ContainedPathHelper.DeleteExistingFileByName(download_directory, safeFileName);
        }
        catch (ArgumentException)
        {
        }

        return await GetQueueResult();
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

