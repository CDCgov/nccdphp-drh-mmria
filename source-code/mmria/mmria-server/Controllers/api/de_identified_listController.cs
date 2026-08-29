using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Http;
using mmria.common.SharedLibraries.MetadataVersion;
using  mmria.server.extension; 
namespace mmria.server;

[Route("api/[controller]")]
public sealed class de_identified_listController: ControllerBase 
{ 

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly IMetadataRepository _metadataRepository;
    public de_identified_listController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        IMetadataRepository metadataRepository
    )
    {
        _metadataRepository = metadataRepository;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Get(string id) 
    { 
        try
        {
            if(!string.IsNullOrWhiteSpace(id) && id.ToLower() == "export")
            {
                return await _metadataRepository.GetDeIdentifiedExportListAsync(db_config);
            }
            else
            {
                return await _metadataRepository.GetDeIdentifiedListAsync(db_config);
            }
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

            if(!string.IsNullOrWhiteSpace(id) && id.ToLower() == "export")
            {
                result = await _metadataRepository.SaveDeIdentifiedExportListAsync(document_json, db_config);
            }
            else
            {
                result = await _metadataRepository.SaveDeIdentifiedListAsync(document_json, db_config);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        }

        return result;
    } 

} 

