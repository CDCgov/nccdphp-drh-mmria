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
    List<mmria.common.couchdb.OverridableConfiguration> _overridableConfigSets;
    List<mmria.common.couchdb.ConfigurationSet> _dbConfigSets;
    common.couchdb.DBConfigurationDetail db_config;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    string host_prefix = null;
    public syncController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        List<mmria.common.couchdb.OverridableConfiguration> overridableConfigSets,
        List<mmria.common.couchdb.ConfigurationSet> dbConfigSets,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        configuration = _configuration;
        _overridableConfigSets = overridableConfigSets;
        _dbConfigSets = dbConfigSets;
        _couchDbHttpClient = couchDbHttpClient;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        configuration = mmria.server.util.MultiTenantConfigHelper.GetConfigurationForTenant(_overridableConfigSets, _configuration, host_prefix);
        db_config = mmria.server.util.MultiTenantConfigHelper.GetDBConfigForTenant(_dbConfigSets, _configuration, host_prefix);
    }

    [HttpGet]
    public string Get()
    {
        string result = null;

        System.Threading.Tasks.Task.Run
        (
            new Action (() =>
            {

                try 
                {
                    
                    mmria.server.utils.c_document_sync_all sync_all = new mmria.server.utils.c_document_sync_all 
                    (
                        db_config.url,
                        db_config.user_name,
                        db_config.user_value,
                        configuration.GetString("metadata_version", host_prefix),
                        db_config,
                        _couchDbHttpClient
                    );

                    sync_all.executeAsync (); 
                }
                catch (Exception ex) 
                {
                    System.Console.WriteLine ($"syncController. error sync_all.execute\n{ex}");
                }
            })
        );
        

        return result;

    } 

} 


#endif