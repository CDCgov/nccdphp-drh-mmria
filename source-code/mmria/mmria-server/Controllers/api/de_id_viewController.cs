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
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server;

[Authorize(Roles  = "committee_member")]
[Route("api/[controller]")]
public sealed class de_id_viewController: ControllerBase
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;

    string host_prefix = null;

    public de_id_viewController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets
    )
    {
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
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
        var cvs = new mmria.server.utils.CaseViewSearch
        (
            db_config, 
            User,
            is_identefied_case
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

