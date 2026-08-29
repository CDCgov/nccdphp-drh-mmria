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
        mmria.common.metadata.Populate_CDC_Instance populate_cdc_instance =
            await mmria.server.util.JsonRequestBodyReader.ReadAsync<mmria.common.metadata.Populate_CDC_Instance>(Request);
        var sanitizedDocument = CreateSanitizedPopulateCdcInstance(populate_cdc_instance);

        if(sanitizedDocument?._id == "populate-cdc-instance")
        {
            try
            {
            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            document_content = Newtonsoft.Json.JsonConvert.SerializeObject(sanitizedDocument, settings);
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
    public async System.Threading.Tasks.Task<mmria.common.metadata.Populate_CDC_Instance> Put()
    {
        var request_message = await mmria.server.util.JsonRequestBodyReader.ReadAsync<mmria.common.metadata.Populate_CDC_Instance>(Request);
        mmria.common.metadata.Populate_CDC_Instance result = new ();
        var safeRequest = CreateSanitizedPopulateCdcInstance(request_message);

        if (safeRequest == null)
        {
            return result;
        }

        try
        {
            result = await _mmriaServicesManager.PutPopulateCDCInstanceToServiceAsync(
                safeRequest,
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

    private static mmria.common.metadata.Populate_CDC_Instance CreateSanitizedPopulateCdcInstance(mmria.common.metadata.Populate_CDC_Instance request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request._id))
        {
            return null;
        }

        return new mmria.common.metadata.Populate_CDC_Instance
        {
            _id = request._id.Trim(),
            _rev = string.IsNullOrWhiteSpace(request._rev) ? null : request._rev.Trim(),
            state_list = request.state_list?
                .Where(item => item != null)
                .Select(item => new mmria.common.metadata.State_List_Item
                {
                    is_included = item.is_included,
                    prefix = string.IsNullOrWhiteSpace(item.prefix) ? null : item.prefix.Trim(),
                    name = string.IsNullOrWhiteSpace(item.name) ? null : item.name.Trim()
                })
                .ToList() ?? new List<mmria.common.metadata.State_List_Item>()
        };
    }

}

