#if IS_PMSS_ENHANCED
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

namespace mmria.pmss.server;

[Authorize(Roles  = "abstractor, data_analyst, committee_member, vro")]
[Route("api/[controller]")]
public sealed class case_viewController: ControllerBase 
{  

    IHttpContextAccessor httpContextAccessor;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.Case.ICaseRepository _caseRepository;
    private readonly mmria.common.SharedLibraries.Jurisdiction.IJurisdictionRepository _jurisdictionRepository;

    string host_prefix = null;

    public case_viewController  (
        IHttpContextAccessor p_httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.Case.ICaseRepository caseRepository,
        mmria.common.SharedLibraries.Jurisdiction.IJurisdictionRepository jurisdictionRepository
    )
    {
        httpContextAccessor = p_httpContextAccessor;
        _couchDbHttpClient = couchDbHttpClient;
        _caseRepository = caseRepository;
        _jurisdictionRepository = jurisdictionRepository;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public async Task<mmria.common.model.couchdb.pmss_case_view_response> Get
    (
        System.Threading.CancellationToken cancellationToken,
        int skip = 0,
        int take = 25,
        string sort = "by_date_created",
        string search_key = null,     
        string field_selection = "all",
        bool descending = false,
        string jurisdiction = "all",
        string year_of_death = "all",
        string status = "all",
        string classification = "all",
        string date_of_death_range = "all",
        string date_of_review_range = "all",
        bool include_pinned_cases = false

    ) 
    {

        var User = httpContextAccessor.HttpContext.User;
        var is_identefied_case = true;
        var cvs = new mmria.pmss.server.utils.CaseViewSearch
        (
            db_config, 
            User,
            is_identefied_case,
            include_pinned_cases,
            _couchDbHttpClient,
            _jurisdictionRepository,
            _caseRepository
        );

        var result = await cvs.execute
        (
            cancellationToken,
            skip,
            take,
            sort,
            search_key,
            descending,
            field_selection,
            jurisdiction,
            year_of_death,
            status,
            classification,
            date_of_death_range,
            date_of_review_range
        );


        return result;
    }


    [HttpGet("record-id-list")]
    public async Task<HashSet<string>> GetExistingRecordIds()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        try
        {
            string responseFromServer = await _caseRepository.GetCasesByDateCreatedViewJsonAsync(db_config);

            mmria.common.model.couchdb.pmss_case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.pmss_case_view_response>(responseFromServer);

            foreach (mmria.common.model.couchdb.pmss_case_view_item cvi in case_view_response.rows)
            {
                result.Add(cvi.value.pmssno);

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    [HttpGet("next-pmss-number/{prefix}")]
    public async Task<string> GetNextPMSSNumber
    (
        string prefix
    )
    {
        var result = new List<string>();

        var prefix_array = prefix.Split("-");

        try
        {
            string responseFromServer = await _caseRepository.GetCasesByPmssNumberViewJsonAsync(db_config);

            mmria.common.model.couchdb.pmss_case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.pmss_case_view_response>(responseFromServer);

            foreach (mmria.common.model.couchdb.pmss_case_view_item cvi in case_view_response.rows)
            {
                if(cvi.value.pmssno.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(cvi.value.pmssno);
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return $"{prefix}-{(result.Count + 1).ToString().PadLeft(4,'0')}";
    }

} 

#endif
