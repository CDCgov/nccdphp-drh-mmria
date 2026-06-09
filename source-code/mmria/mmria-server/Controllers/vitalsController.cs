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
using mmria.common.SharedLibraries.Jurisdiction.Manager;


namespace VitalsImport_FileUpload.Controllers;

[Authorize(Roles = "vital_importer")]
public sealed class vitalsController : Controller
{
    private readonly ILogger<vitalsController> _logger;
    private readonly IConfiguration _appConfiguration;

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.server.util.TenantCatalog _tenantCatalog;
    private readonly JurisdictionManager _jurisdictionManager;

    public vitalsController
    (
        ILogger<vitalsController> logger,
        IConfiguration appConfiguration,
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.server.util.TenantCatalog tenantCatalog,
        JurisdictionManager jurisdictionManager
    )
    {
        _logger = logger;
        _appConfiguration = appConfiguration;
        _tenantCatalog = tenantCatalog;
        _jurisdictionManager = jurisdictionManager;
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
            result = await _jurisdictionManager.GetJurisdictionTreeAsync(detail);

        }
        catch(Exception ex) 
        {
            var message = $"{ex}";
             
             
            System.Console.WriteLine($"{ex}");
        }


        return EscapedJsonResultFactory.Create(result);
    }

}

