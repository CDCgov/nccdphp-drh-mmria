using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Dynamic;
using mmria.common;
using Microsoft.Extensions.Configuration;
using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using mmria.common.model.couchdb.recover_doc;

namespace mmria.server;

	
[Route("api/[controller]")]
public sealed class caseRevisionListController: ControllerBase 
{ 
    private readonly mmria.server.util.RequestTenantRuntime _tenantRuntime;
    private readonly mmria.server.util.TenantCatalog _tenantCatalog;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.Case.ICaseRepository _caseRepository;

    public caseRevisionListController
    (
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.server.util.TenantCatalog tenantCatalog,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.Case.ICaseRepository caseRepository
    )
    {
        _tenantRuntime = tenantRuntime;
        _tenantCatalog = tenantCatalog;
        _couchDbHttpClient = couchDbHttpClient;
        _caseRepository = caseRepository;
    }
    
    [Authorize(Roles  = "installation_admin")]
    [HttpGet]
    public async Task<All_Revs> Get(string jurisdiction_id, string case_id) 
    { 
        try
        {
            _ = _tenantRuntime;
            var config = _tenantCatalog.TryResolveDbConfig(jurisdiction_id);
            if (config == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace (case_id)) 
            {
                string responseFromServer = await _caseRepository.GetCaseRevisionsRawAsync(case_id, config);

                var response_split = responseFromServer.Split("\r\n");
                
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<All_Revs>(response_split[3]);

                return result;
            } 

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    } 




} 


