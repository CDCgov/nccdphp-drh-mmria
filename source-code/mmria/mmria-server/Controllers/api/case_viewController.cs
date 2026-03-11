#if !IS_PMSS_ENHANCED
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

[Authorize(Roles  = "abstractor, data_analyst")]
[Route("api/[controller]")]
public sealed class case_viewController: ControllerBase 
{  

    mmria.common.couchdb.OverridableConfiguration configuration;
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;

    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    string host_prefix = null;

    public case_viewController  (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
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
        string pregnancy_relatedness ="all",
        string date_of_death_range = "all",
        string date_of_review_range = "all",
        bool include_pinned_cases = false

    ) 
    {
    
        var is_identefied_case = true;
        var cvs = new mmria.common.SharedLibraries.CaseView.CaseViewManager
        (
            db_config, 
            User,
            is_identefied_case,
            include_pinned_cases,
            _couchDbHttpClient
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
            pregnancy_relatedness,
            date_of_death_range,
            date_of_review_range
        );


        return result;
    }



    [HttpGet("record-id-list")]
    public async Task<System.Collections.Generic.List<string>> GetRecordIdList(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            var cvs = new mmria.common.SharedLibraries.CaseView.CaseViewManager
            (
                db_config,
                User,
                true,
                false,
                _couchDbHttpClient
            );

            return await cvs.GetRecordIdListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return new System.Collections.Generic.List<string>();
    }

   

    [HttpGet("offline-documents")]
    public async Task<mmria.common.model.couchdb.case_view_response> GetOfflineDocuments
    (
        System.Threading.CancellationToken cancellationToken,
        int skip = 0,
        int take = 25,
        string sort = "by_date_created",
        bool descending = false
    )
    {
        try
        {
            var current_user = User.Identity?.Name;
            var cvs = new mmria.common.SharedLibraries.CaseView.CaseViewManager
            (
                db_config,
                User,
                true,
                false,
                _couchDbHttpClient
            );

            return await cvs.GetOfflineDocumentsAsync(current_user, skip, take, sort, descending);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in GetOfflineDocuments: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new mmria.common.model.couchdb.case_view_response();
        }
    }

    public async Task<HashSet<string>> GetExistingRecordIds()
    {
        var cvs = new mmria.common.SharedLibraries.CaseView.CaseViewManager
        (
            db_config,
            User,
            true,
            false,
            _couchDbHttpClient
        );

        return await cvs.GetExistingRecordIdsAsync();
    }

} 

#endif
