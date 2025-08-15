using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace mmria.server.utils;

public sealed class SessionSummaryItem
{
    public SessionSummaryItem()
    {
        rpt_date = new List<int>();
    }
    public string host_name {get; set; }
    public List<int> rpt_date{get; set; }
    public int total {get; set; }
}

sealed class SSIComparer : IComparer<SessionSummaryItem>
{
    public int Compare(SessionSummaryItem x, SessionSummaryItem y)
    {
        
        if (x == null || y == null)
        {
            return 0;
        }

        if (x.host_name == null || y.host_name == null)
        {
            return 0;
        }
        
        // "CompareTo()" method
        return x.host_name.CompareTo(y.host_name);
        
    }
}

public sealed class SessionSummary
{

/*
{
{
"_id": "0002c370-c442-4d2c-b1a3-9cf13bcded67",
"_rev": "2-c6e6e07da856a43e305cb362dfb7e174",
"data_type": "session",
"date_created": "2022-01-24T23:32:31.2479542+00:00",
"date_last_updated": "2022-01-24T23:32:31.2479542+00:00",
"date_expired": "2022-01-24T23:32:34.6457199+00:00",
"is_active": true,
"user_id": "user1",
"ip": "10.10.10.115",
"session_event_id": "fb4c4e9a-abfb-4056-828c-3f3d187acd13",
"role_list": [
"cdc_admin",
"abstractor",
"committee_member"
],
"data": {}
}
}
*/
    public sealed class SessionItem
    {
        public string _id {get;set;}
        public string _rev {get;set;}
        public string data_type {get;set;}
        public DateTime date_created {get;set;}

        public DateTime date_last_updated {get;set;}

        public DateTime date_expired {get;set;}
        public string user_id {get;set;}
        public string ip {get;set;}
        public int? action_result {get;set;}

        public string session_event_id {get;set;}

        string[] role_list {get;set;}

        Dictionary<string,object> data  {get;set;}

    }



    mmria.common.couchdb.ConfigurationSet ConfigDB;

    public SessionSummary(mmria.common.couchdb.ConfigurationSet p_config_db)
    {
        ConfigDB = p_config_db;
    }

    public async Task<List<SessionSummaryItem>> execute(System.Threading.CancellationToken cancellationToken)
    {

        var result = new List<SessionSummaryItem>();
        
        var record_count_result = new Dictionary<string, SessionSummaryItem>(System.StringComparer.OrdinalIgnoreCase);
        var record_count_task_list = new List<Task>();
        //var jurisdiction_count_task_list = new List<Task>();

        var current_date = System.DateTime.Now;

        foreach(var config in ConfigDB.detail_list)
        {

            cancellationToken.ThrowIfCancellationRequested();

            var prefix = config.Key.ToUpper();

            if(prefix == "VITAL_IMPORT") continue;

            var sessionSummaryItem = new SessionSummaryItem();
            sessionSummaryItem.host_name = prefix;
            record_count_result.Add(prefix, sessionSummaryItem);
            
            record_count_task_list.Add(GetSessionCount(cancellationToken, prefix, config.Value, sessionSummaryItem));
            
        }


        await Task.WhenAll(record_count_task_list);
        cancellationToken.ThrowIfCancellationRequested();
        //var user_count_call_results = user_count_responses.Where(r => !string.IsNullOrWhiteSpace(r)); //filter out any null values


        //await Task.WhenAll(jurisdiction_count_task_list);
        //cancellationToken.ThrowIfCancellationRequested();
        //var jurisdiction_count_call_results = jurisdiction_count_responses.Where(r => !string.IsNullOrWhiteSpace(r)); //filter out any null values


        foreach(var kvp in record_count_result)
        {
            result.Add(kvp.Value);
        }

        result.Sort(new SSIComparer());

        return result;
    }

    public async System.Threading.Tasks.Task GetSessionCount(System.Threading.CancellationToken cancellationToken, string p_id, mmria.common.couchdb.DBConfigurationDetail p_config_detail, SessionSummaryItem p_result) 
    { 
        try
        {
            string request_string = $"{p_config_detail.url}/{p_config_detail.prefix}session/_design/session_sortable/_view/by_date_created?descending=true&limit=500";


            cancellationToken.ThrowIfCancellationRequested();
            var user_curl = new cURL("GET",null,request_string,null, p_config_detail.user_name, p_config_detail.user_value);
            string responseFromServer = await user_curl.executeAsync();

            cancellationToken.ThrowIfCancellationRequested();

            var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.view_response<SessionItem>>(responseFromServer);

            var current_day = System.DateTime.Now.Day;

            var cut_off_date = System.DateTime.Now.AddDays(-30);

            for(var i = 0; i < 30; i++)
            {
                p_result.rpt_date.Add(0);
            }
            
            for(var i = 0; i < case_view_response.rows.Count; i++)
            {
                var row = case_view_response.rows[i];
                if(row.value.date_created >= cut_off_date)
                {
                    var diff = System.DateTime.Now - row.value.date_created;
                    var row_index = diff.Days;
                    p_result.rpt_date[row_index]++;
                }
                else
                {
                    break;
                }
            }

        }
        catch(System.Exception)
        {
            p_result.total = -1;
        }
    }
    

}
