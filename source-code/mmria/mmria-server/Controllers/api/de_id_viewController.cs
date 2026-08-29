#if !IS_PMSS_ENHANCED
      
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using mmria.common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

using  mmria.server.extension;
using mmria.common.SharedLibraries.Jurisdiction;

namespace mmria.server;

[Authorize(Roles  = "committee_member")]
[Route("api/[controller]")]
public sealed class de_id_viewController: ControllerBase
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly IJurisdictionRepository _jurisdictionRepository;

    string host_prefix = null;

    public de_id_viewController
    (
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        IJurisdictionRepository jurisdictionRepository
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        _jurisdictionRepository = jurisdictionRepository;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public async Task<mmria.common.model.couchdb.case_view_response> Get
    (
        System.Threading.CancellationToken cancellationToken,
        int skip = 0,
        int take = 25,
        string sort = "by_date_created",
        string search_key = null,
        bool descending = false,
        string case_status = "all",
        string field_selection = "all",
        string pregnancy_relatedness ="all"
    ) 
    {

        const bool is_identefied_case = false;
        var cvs = new mmria.common.SharedLibraries.CaseView.CaseViewManager
        (
            db_config, 
            User,
            is_identefied_case,
            false,
            _couchDbHttpClient,
            _jurisdictionRepository
        );
        

        var result = await cvs.execute
        (
            cancellationToken,
            skip,
            take,
            sort,
            search_key,
            descending,
            case_status,
            field_selection,
            pregnancy_relatedness
        );
        

        return result;
    }
}
#endif

