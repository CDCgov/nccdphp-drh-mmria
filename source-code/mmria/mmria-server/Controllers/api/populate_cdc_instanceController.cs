using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;

namespace mmria.server.Controllers;

[Route("api/[controller]")]
public sealed class populate_cdc_instanceController : ControllerBase
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.SharedLibraries.MMRIAServices.Manager.MMRIAServicesManager _mmriaServicesManager;

    public populate_cdc_instanceController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.MMRIAServices.Manager.MMRIAServicesManager mmriaServicesManager
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        _mmriaServicesManager = mmriaServicesManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [Authorize(Roles = "cdc_admin")]
    [HttpGet]
    public async Task<mmria.common.metadata.Populate_CDC_Instance> Get()
    {
        mmria.common.metadata.Populate_CDC_Instance result = new();
        try
        {
            result = await _mmriaServicesManager.GetPopulateCDCInstanceAsync(
                db_config,
                configuration.GetString("vitals_url", host_prefix).Replace("Message/IJESet", "PopulateCDCInstance"),
                configuration.GetString("vital_service_key", host_prefix)
            );
                
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }
    [Authorize(Roles = "cdc_admin")]

    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (

    //mmria.common.metadata.Add_Attachement add_attachement
    )
    {
        string document_content;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response();

        try
        {
        System.IO.Stream dataStream0 = this.Request.Body;
        //dataStream0.Seek(0, System.IO.SeekOrigin.Begin);
        System.IO.StreamReader reader0 = new System.IO.StreamReader(dataStream0);

        document_content = await reader0.ReadToEndAsync();

        mmria.common.metadata.Populate_CDC_Instance populate_cdc_instance = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.metadata.Populate_CDC_Instance>(document_content);

        if(populate_cdc_instance._id == "populate-cdc-instance")
        {
            try
            {
            result = await _mmriaServicesManager.SavePopulateCDCInstanceDocumentAsync(document_content, db_config);

            }
            catch (Exception ex)
            {
            Console.WriteLine(ex);
            }
        }

        if (!result.ok)
        {

        }

        }
        catch (Exception ex)
        {
        Console.WriteLine(ex);
        }

        return result;
    }

    [Authorize(Roles  = "cdc_admin")]
    [HttpPut]
    public async System.Threading.Tasks.Task<mmria.common.metadata.Populate_CDC_Instance> Post([FromBody] mmria.common.metadata.Populate_CDC_Instance request_message) 
    { 
        mmria.common.metadata.Populate_CDC_Instance result = new ();

        try
        {
            result = await _mmriaServicesManager.PutPopulateCDCInstanceToServiceAsync(
                request_message,
                configuration.GetString("vitals_url", host_prefix).Replace("Message/IJESet", "PopulateCDCInstance"),
                configuration.GetString("vital_service_key", host_prefix)
            );

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
            result.transfer_status_number = 2;
            result.transfer_result = ex.Message;
            
        }

        return result;
    } 

    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

}

