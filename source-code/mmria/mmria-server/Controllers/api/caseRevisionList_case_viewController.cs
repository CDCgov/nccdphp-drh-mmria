using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common.functional;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;    
	
namespace mmria.server;

[Authorize(Roles  = "installation_admin")]
[Route("api/[controller]")]
public sealed class caseRevisionList_case_viewController: ControllerBase 
{  
    mmria.common.couchdb.OverridableConfiguration configuration;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    delegate bool is_valid_predicate(mmria.common.model.couchdb.case_view_item item);
 
    public caseRevisionList_case_viewController
    (

        mmria.common.couchdb.OverridableConfiguration _configuration,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        configuration = _configuration;
        _couchDbHttpClient = couchDbHttpClient;
    }


    [HttpGet]
    public async Task<mmria.common.model.couchdb.case_view_response> Get
    (
        System.Threading.CancellationToken cancellationToken,
        string jurisdiction_id,
        string search_key
    ) 
    {
        var config = configuration.GetDBConfig(jurisdiction_id);
        var caseViewManager = new mmria.common.SharedLibraries.CaseView.CaseViewManager(
            config,
            User,
            true,
            false,
            _couchDbHttpClient
        );

        return await caseViewManager.GetCaseRevisionListAsync(search_key);

      
    }

} 

