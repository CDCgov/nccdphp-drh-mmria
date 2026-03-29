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
public sealed class pinned_casesController : ControllerBase
{
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public pinned_casesController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [Authorize(Roles = "abstractor")]
    [HttpGet]
    public async Task<mmria.common.model.couchdb.pinned_case_set> Get()
    {
        var caseViewManager = new mmria.common.SharedLibraries.CaseView.CaseViewManager(
            db_config,
            User,
            true,
            false,
            _couchDbHttpClient
        );
        mmria.common.model.couchdb.pinned_case_set result = await caseViewManager.GetOrCreatePinnedCaseSetAsync();
        return result;
    }

    [Authorize(Roles = "abstractor")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post
    (


    )
    {
        string document_content;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response();

        try
        {
            System.IO.Stream dataStream0 = this.Request.Body;

            System.IO.StreamReader reader0 = new System.IO.StreamReader(dataStream0);

            document_content = await reader0.ReadToEndAsync();

            var pin_case_message = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.pin_case_message>(document_content);

            var caseViewManager = new mmria.common.SharedLibraries.CaseView.CaseViewManager(
                db_config,
                User,
                true,
                false,
                _couchDbHttpClient
            );
            result = await caseViewManager.ApplyPinnedCaseMessageAsync(pin_case_message);

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

    [Authorize(Roles = "jurisdiction_admin")]
    [HttpPut]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Put
    (

    )
    {
        string document_content;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response();

        try
        {
            System.IO.Stream dataStream0 = this.Request.Body;

            System.IO.StreamReader reader0 = new System.IO.StreamReader(dataStream0);

            document_content = await reader0.ReadToEndAsync();

            var pin_case_message = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.pin_case_message>(document_content);

            if(pin_case_message.user_id == "everyone")
            {

                var caseViewManager = new mmria.common.SharedLibraries.CaseView.CaseViewManager(
                    db_config,
                    User,
                    true,
                    false,
                    _couchDbHttpClient
                );
                result = await caseViewManager.ApplyPinnedCaseMessageAsync(pin_case_message);

                if (!result.ok)
                {

                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }


}

