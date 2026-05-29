using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
using mmria.common.SharedLibraries.MetadataVersion.Manager;

namespace mmria.server;

[Route("api/[controller]")]
public sealed class export_list_managerController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly MetadataVersionManager _metadataVersionManager;
    public export_list_managerController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        MetadataVersionManager metadataVersionManager
    )
    {
        _metadataVersionManager = metadataVersionManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Get() 
    { 
        try
        {
            return await _metadataVersionManager.GetExportStandardListAsync(db_config);

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    } 

    [Authorize(Roles = "form_designer, cdc_admin")]
    [Route("{id?}")]
    [HttpPost]
    [HttpPut]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post(string id) 
    { 
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {

            System.IO.Stream dataStream0 = this.Request.Body;
            System.IO.StreamReader reader0 = new System.IO.StreamReader (dataStream0);

            string document_json = await reader0.ReadToEndAsync ();

            result = await _metadataVersionManager.SaveExportStandardListAsync(document_json, db_config);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        }

        return result;
    } 
} 


