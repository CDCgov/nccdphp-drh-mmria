#if !IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 

namespace mmria.server;

[Authorize(Roles  = "installation_admin")]
[Route("api/[controller]")]
public sealed class syncController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.common.SharedLibraries.MMRIARebuild.Manager.MMRIARebuildManager _mmriaRebuildManager;
    string host_prefix = null;
    public syncController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.MMRIARebuild.Manager.MMRIARebuildManager mmriaRebuildManager
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        _mmriaRebuildManager = mmriaRebuildManager;
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [HttpGet]
    public string Get()
    {
        string result = null;

        System.Threading.Tasks.Task.Run(async () =>
        {
            try 
            {
                string rebuildServiceUrl = mmria.common.SharedLibraries.MMRIARebuild.Manager.MMRIARebuildManager.BuildServiceUrl(
                    configuration.GetString("vitals_url", host_prefix));

                await _mmriaRebuildManager.QueueRebuildOnServiceAsync(
                    new mmria.common.SharedLibraries.MMRIARebuild.Model.MMRIARebuildRequest
                    {
                        tenant = host_prefix,
                        source = "manual",
                        requested_behavior = "resume"
                    },
                    rebuildServiceUrl,
                    configuration.GetString("vital_service_key", host_prefix));
            }
            catch (Exception ex) 
            {
                System.Console.WriteLine ($"syncController. error sync_all.execute\n{ex}");
            }
        });
        

        return result;

    } 

} 


#endif
