using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using mmria.server.model;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;


namespace VitalsImport_FileUpload.Controllers;

[Authorize(Roles = "vital_importer")]
public sealed class vitalsController : Controller
{
    private readonly ILogger<vitalsController> _logger;

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.server.util.TenantCatalog _tenantCatalog;

    public vitalsController
    (
        ILogger<vitalsController> logger,
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.server.util.TenantCatalog tenantCatalog,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _logger = logger;
        _couchDbHttpClient = couchDbHttpClient;
        _tenantCatalog = tenantCatalog;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();
        db_config = tenantRuntime.RequireDbConfig();
    }

    
    public IActionResult Index()
    {
        var model = new FileUploadModel();
        return View(model);
    }

    [HttpGet]
    public IActionResult FileUpload()
    {
        var model = new FileUploadModel();
        return View(model);
    }

    /*

    [HttpGet]
    public async Task<JsonResult> GetFolderList(string h)
    {
        mmria.common.model.couchdb.jurisdiction_tree result = null;

        try
        {
            
            string jurisdiction_tree_url = $"{db_config.url}/jurisdiction/jurisdiction_tree";
            if(!string.IsNullOrWhiteSpace(db_config.prefix))
            {
                jurisdiction_tree_url = $"{db_config.url}/{db_config.prefix}jurisdiction/jurisdiction_tree";
            }

            var jurisdiction_curl = new cURL("GET", null, jurisdiction_tree_url, null, db_config.user_name, db_config.user_value);
            string response_from_server = await jurisdiction_curl.executeAsync ();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.jurisdiction_tree>(response_from_server);

        }
        catch(Exception ex) 
        {
            System.Console.WriteLine($"{ex}");
        }


        return Json(result);
    }*/

    [HttpGet]
    public async Task<JsonResult> GetJurisdictionTree(string j)
    {

        mmria.common.model.couchdb.jurisdiction_tree result = null;

        try{
            var detail = _tenantCatalog.TryResolveDbConfig(j?.ToLower());
            if (detail == null)
            {
                return Json(result);
            }
            string jurisdiction_tree_url = $"{detail.url}/jurisdiction/jurisdiction_tree";
            if(!string.IsNullOrWhiteSpace(detail.prefix))
            {
                jurisdiction_tree_url = $"{detail.url}/{detail.prefix}jurisdiction/jurisdiction_tree";
            }

            string response_from_server = await _couchDbHttpClient.ExecuteAsync("GET", jurisdiction_tree_url, null, detail.user_name, detail.user_value);

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.jurisdiction_tree>(response_from_server);

        }
        catch(Exception ex) 
        {
            var message = $"{ex}";
             
             
            System.Console.WriteLine($"{ex}");
        }


        return Json(result);
    }

}

