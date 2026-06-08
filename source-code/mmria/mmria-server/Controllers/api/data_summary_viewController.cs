
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
using mmria.common.SharedLibraries.DataSummary.Manager;
namespace mmria.server;

[Authorize(Roles  = "abstractor, data_analyst")]
[Route("/api/data-summary/{skip}")]
public sealed class data_summary_viewControllerController: ControllerBase 
{  

    struct Selector_Struc
    {
        //public System.Dynamic.ExpandoObject selector;
        public System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>> selector;

        public string use_index;

        public int limit;

        public int skip;
    } 
    
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly DataSummaryManager _dataSummaryManager;

    public data_summary_viewControllerController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        DataSummaryManager dataSummaryManager
    )
    {
        _dataSummaryManager = dataSummaryManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }
    public async Task<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.SharedLibraries.MMRIARebuild.Model.SummaryReport.FrequencySummaryDocument>> Get(string skip)
    {
        var result = new mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.SharedLibraries.MMRIARebuild.Model.SummaryReport.FrequencySummaryDocument>();
        
        try
        {
            result = await _dataSummaryManager.GetYearOfDeathSummaryAsync(skip, db_config);
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }




} 


