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
    mmria.common.couchdb.OverridableConfiguration configuration;

    public caseRevisionListController
    (

        mmria.common.couchdb.OverridableConfiguration p_config_db
    )
    {
        configuration = p_config_db;
    }
    
    [Authorize(Roles  = "installation_admin")]
    [HttpGet]
    public async Task<All_Revs> Get(string jurisdiction_id, string case_id) 
    { 
        try
        {
            var config = configuration.GetDBConfig(jurisdiction_id);

            string all_revs_url = $"{config.url}/{config.prefix}mmrds/{case_id}?revs=true&open_revs=all";

            if (!string.IsNullOrWhiteSpace (case_id)) 
            {
                var case_curl = new cURL("GET", null, all_revs_url, null, config.user_name, config.user_value);
                string responseFromServer = case_curl.execute();

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


