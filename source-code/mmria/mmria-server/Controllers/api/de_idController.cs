using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using mmria.common.SharedLibraries.DeIdentified;

using  mmria.server.extension; 
namespace mmria.server;

[Authorize(Roles  = "committee_member")]
[Route("api/[controller]")]
public sealed class de_idController: ControllerBase 
{     
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly IDeIdentifiedRepository _deIdentifiedRepository;

    public de_idController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        IDeIdentifiedRepository deIdentifiedRepository
    )
    {
        _deIdentifiedRepository = deIdentifiedRepository;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    public async Task<System.Dynamic.ExpandoObject> Get(string case_id = null) 
    { 
        try
        {
            string responseFromServer;

            if (!string.IsNullOrWhiteSpace (case_id)) 
            {
                responseFromServer = await _deIdentifiedRepository.GetDocumentJsonAsync(case_id, db_config);
            }
            else
            {
                responseFromServer = await _deIdentifiedRepository.GetAllDocumentsJsonAsync(true, db_config);
            }

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (responseFromServer);

            return result;
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
    } 

} 


