using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mmria.common.model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 

namespace mmria.server;

[Route("api/[controller]")]
public sealed class nioshController: ControllerBase
{ 
    public record Record_Id_Response
    {
        public bool ok { get; init;}
        public bool is_unique { get; init;}
    }    
    
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public nioshController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [HttpGet]
    public async Task<mmria.common.niosh.NioshResult> Get(string o = null, string i = null)
    {
        var result = new mmria.common.niosh.NioshResult();
        try
        {        

            var builder = new System.Text.StringBuilder();
            builder.Append("https://wwwn.cdc.gov/nioccs/IOCode?n=3");
            var has_occupation = false;
            var has_industry = false;

            if(!string.IsNullOrWhiteSpace(o))
            {
                has_occupation = true;
                builder.Append($"&o={o}");
            }

            if(!string.IsNullOrWhiteSpace(i))
            {
                has_industry = true;
                builder.Append($"&i={i}");
            }

    
            if(has_occupation || has_industry)
            {
                var niosh_url = builder.ToString();

                var niosh_curl = new mmria.getset.cURL("GET", null, niosh_url, null);

                try
                {
                    string responseFromServer = await niosh_curl.executeAsync();

                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.niosh.NioshResult>(responseFromServer);
                }
                catch
                {
                    result.is_error = true;
                }
                
            }
            //{"Industry": [{"Code": "611110","Title": "Elementary and Secondary Schools","Probability": "9.999934E-001"},{"Code": "611310","Title": "Colleges, Universities, and Professional Schools","Probability": "2.598214E-006"},{"Code": "009990","Title": "Insufficient information","Probability": "2.312557E-006"}],"Occupation": [{"Code": "00-9900","Title": "Insufficient Information","Probability": "9.999897E-001"},{"Code": "11-9032","Title": "Education Administrators, Elementary and Secondary School","Probability": "6.550550E-006"},{"Code": "53-3022","Title": "Bus Drivers, School or Special Client","Probability": "4.932875E-007"}],"Scheme": "NAICS 2012 and SOC 2010"}
            //return result;
        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } 
} 


