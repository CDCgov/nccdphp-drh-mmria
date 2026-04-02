using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;   
using mmria.common.cvs;
using mmria.server.util;

namespace mmria.server;

[Authorize]
[Route("api/[controller]")]
public sealed class cvsAPIController: ControllerBase 
{ 

    public sealed class CVS_File_Status
    {
        public CVS_File_Status () {}

        public string file_status { get;set; }
        public string updated_lat { get;set; }
        public string updated_lon { get;set; }

        public string updated_year { get;set; }

        public bool is_valid_address { get;set; } = true;
        public bool is_valid_year { get;set; } = true;

    }

    string folder_name = null;

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly System.Net.Http.HttpClient _externalHttpClient;
    private readonly mmria.common.SharedLibraries.CVS.Manager.CVSManager _cvsManager;
    public cvsAPIController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.CVS.Manager.CVSManager cvsManager
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        _cvsManager = cvsManager;
        var httpClientFactory = new mmria.common.SimpleHttpClientFactory();
        _externalHttpClient = httpClientFactory.CreateClient("external");
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();

        this.folder_name = ContainedPathHelper.ResolveContainedDirectoryPath(configuration.GetString("export_directory", host_prefix), "csv");

        System.IO.Directory.CreateDirectory(this.folder_name);

    }

    private static string GetCvsPdfFileName(string id)
    {
        var safeId = ContainedPathHelper.ValidateContainedName(id, nameof(id));
        return ContainedPathHelper.ValidateContainedName($"CVS-{safeId}.pdf", nameof(id));
    }


    [Authorize(Roles  = "abstractor,data_analyst,committee_member")]
    [HttpGet("{id}")]
    public async System.Threading.Tasks.Task<ActionResult> Get (string id)
    {


        var file_name = GetCvsPdfFileName(id);
        var file_path = ContainedPathHelper.ResolveContainedFilePath(folder_name, file_name);

        if(System.IO.File.Exists(file_path))
        {
            byte[] fileBytes = await GetFile(file_path);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, file_name);
        }
        else
        {
            return NotFound();
        }


    }


    
    [Authorize(Roles  = "abstractor,data_analyst,committee_member")]
    [HttpPost]
    public async Task<IActionResult> Post
    (
        [FromBody] post_payload post_payload
    ) 
    { 
        var is_abstractor = false;

        foreach(var role in User.Identities.First(u => u.IsAuthenticated &&  u.HasClaim(c => c.Type == ClaimTypes.Name)).Claims.Where(c=> c.Type == ClaimTypes.Role))
        {
            switch(role.Value)
            {
                case "abstractor":
                    is_abstractor = true;
                break;

            }
        }
  

        const int year_difference_limit = 9;

        IActionResult result = null;
        var response_string = string.Empty;
        System.Collections.Generic.IDictionary<string,object> responseDictionary = null;
        var cvs = configuration.GetCVSConfigurationDetail();

        var base_url = cvs.cvs_api_url;

        try
        {
            

            switch(post_payload.action)
            {
                case "server":
                    response_string = await _cvsManager.GetServerStatusAsync(cvs);
                    System.Console.WriteLine(response_string);

                    result = Ok(response_string);

    
                break;
                case "data":
                    if(is_abstractor)
                    {
                        var tc = await _cvsManager.GetAllDataAsync(post_payload, cvs);

                        result =  Ok(tc);

        
                    }

                    break;

                case "dashboard":

                    var file_status_result = new CVS_File_Status();
                    var dashboardResult = await _cvsManager.GetDashboardAsync(post_payload, cvs, db_config);
                    file_status_result.file_status = dashboardResult.file_status;
                    file_status_result.updated_lat = dashboardResult.updated_lat;
                    file_status_result.updated_lon = dashboardResult.updated_lon;
                    file_status_result.updated_year = dashboardResult.updated_year;
                    file_status_result.is_valid_address = dashboardResult.is_valid_address;
                    file_status_result.is_valid_year = dashboardResult.is_valid_year;
                    if (dashboardResult.PdfBytes != null)
                    {
                        var file_name = GetCvsPdfFileName(post_payload.id);
                        var file_path = ContainedPathHelper.ResolveContainedFilePath(folder_name, file_name);
                        System.IO.File.WriteAllBytes(file_path, dashboardResult.PdfBytes);
                    }
                    result = Ok(file_status_result);
                    
                    break;
            }
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController  POST\n{ex}");
            
            return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: "The CVS API request failed.",
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );
        }


        if(result == null)
        {
            //return JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(response_string);
            return Ok(JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(response_string));
        }
        else
        {
            //return null;
            return result;
        }
    }

    async Task<byte[]> GetFile(string s)
    {
        byte[] data;
        int br;
        int fs_length;

        using(FileStream fs = new FileStream (s, FileMode.Open, FileAccess.Read))
        {
            fs_length = (int) fs.Length;
            data = new byte[fs.Length];
            br = await fs.ReadAsync(data, 0, data.Length);
        }
        if (br != (int) fs_length)
            throw new System.IO.IOException(s);
        return data;
    }


} 


