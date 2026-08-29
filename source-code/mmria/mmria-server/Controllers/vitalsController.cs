using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using mmria.server.model;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;
using mmria.server.util;
using mmria.common.SharedLibraries.Jurisdiction;


namespace VitalsImport_FileUpload.Controllers;

[Authorize(Roles = "vital_importer")]
public sealed class vitalsController : Controller
{
    private readonly ILogger<vitalsController> _logger;
    private readonly IConfiguration _appConfiguration;

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.server.util.TenantCatalog _tenantCatalog;
    private readonly IJurisdictionRepository _jurisdictionRepository;

    public vitalsController
    (
        ILogger<vitalsController> logger,
        IConfiguration appConfiguration,
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.server.util.TenantCatalog tenantCatalog,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        IJurisdictionRepository jurisdictionRepository
    )
    {
        _logger = logger;
        _appConfiguration = appConfiguration;
        _couchDbHttpClient = couchDbHttpClient;
        _tenantCatalog = tenantCatalog;
        _jurisdictionRepository = jurisdictionRepository;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();
        db_config = tenantRuntime.RequireDbConfig();
    }

    private void PopulateVitalsUploadViewData()
    {
        TempData["vitals_import_additional_tenants"] = _appConfiguration["mmria_settings:vitals_import_additional_tenants"] ?? string.Empty;
    }

    
    public IActionResult Index()
    {
        PopulateVitalsUploadViewData();
        var model = new FileUploadModel();
        return View(model);
    }

    [HttpGet]
    public IActionResult FileUpload()
    {
        PopulateVitalsUploadViewData();
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
    public async Task<IActionResult> GetJurisdictionTree(string j)
    {

        mmria.common.model.couchdb.jurisdiction_tree result = null;

        try{
            var detail = _tenantCatalog.TryResolveDbConfig(j?.ToLower());
            if (detail == null)
            {
                return EscapedJsonResultFactory.Create(result);
            }

            result = await _jurisdictionRepository.GetJurisdictionTreeAsync(detail);
        }
        catch(Exception ex) 
        {
            System.Console.WriteLine($"{ex}");
        }


        return EscapedJsonResultFactory.Create(result);
    }

}

