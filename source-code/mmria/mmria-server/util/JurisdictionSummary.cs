using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace mmria.server.utils;

public sealed class ItemCount
{
    public ItemCount(){}

    public string host_name {get; set; }

    public string folder_name {get; set; } = "/";
    
    public int total{get; set; }
}
public sealed class JurisdictionSummaryItem
{
    public JurisdictionSummaryItem(){}
    public string host_name {get; set; }

    public string folder_name {get; set; } = "/";
    public string rpt_date{get; set; }
    public int num_recs{get; set; }
    public int num_users_unq{get; set; }
    public int num_users_ja{get; set; }
    public int num_users_abs{get; set; }
    public int num_user_anl{get; set; }
    public int num_user_cm{get; set; }
}

sealed class JSIComparer : IComparer<JurisdictionSummaryItem>
{
    public int Compare(JurisdictionSummaryItem x, JurisdictionSummaryItem y)
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

        if(x.host_name != y.host_name)
        {
            return x.host_name.CompareTo(y.host_name);
        }
        else
        {
            return x.folder_name.CompareTo(y.folder_name);
        }
        
    }
}

public sealed class JurisdictionSummary
{

    mmria.common.couchdb.ConfigurationSet ConfigDB;

    public JurisdictionSummary(mmria.common.couchdb.ConfigurationSet p_config_db)
    {

        ConfigDB = p_config_db;
    }

    public async Task<List<JurisdictionSummaryItem>> execute
    (
        System.Threading.CancellationToken cancellationToken
    )
    {

        var result = new Dictionary<string, JurisdictionSummaryItem>(System.StringComparer.OrdinalIgnoreCase);
        var user_count_result = new Dictionary<string, ItemCount>(System.StringComparer.OrdinalIgnoreCase);
        var record_count_result = new Dictionary<string, ItemCount>(System.StringComparer.OrdinalIgnoreCase);
        var user_count_task_list = new List<Task>();
        var record_count_task_list = new List<Task>();
        //var jurisdiction_count_task_list = new List<Task>();

        var current_date = System.DateTime.Now;

        foreach(var config in ConfigDB.detail_list)
        {

            cancellationToken.ThrowIfCancellationRequested();

            var prefix = config.Key.ToUpper();
            string exclude_jurisdiction = "";



            if(prefix == "VITAL_IMPORT") continue;

            if(prefix == "NY"|| prefix == "PA")
            {
                {
                    
                        
                    if(prefix == "NY")
                    {
                        exclude_jurisdiction = "/NYC";
                        
                    }

                    if(prefix == "PA")
                    {
                        exclude_jurisdiction = "/PHILADELPHIA";
                        
                    }
                    
                    var jsi = new JurisdictionSummaryItem();
                    jsi.rpt_date = $"{current_date.Month}/{current_date.Day}/{current_date.Year}";
                    jsi.host_name = prefix;

                    result.Add(prefix, jsi);

                    var usr_count = new ItemCount();
                    usr_count.host_name = prefix;
                    
                    user_count_result.Add(prefix, usr_count);

                    var record_count = new ItemCount();
                    record_count.host_name = prefix;
                    
                    record_count_result.Add(prefix, record_count);

                    user_count_task_list.Add(GetUserCount(cancellationToken, prefix, config.Value, usr_count, jsi, exclude_jurisdiction));
                    record_count_task_list.Add(GetCaseCount(cancellationToken, prefix, config.Value, record_count, exclude_jurisdiction));
                }
                
                {
                    var key_name = prefix;
                    var folder_name = "/";
                    if(prefix == "NY")
                    {
                            exclude_jurisdiction = "/";
                            key_name = "NYC";
                            folder_name = "/NYC";
                    }

                    if(prefix == "PA")
                    {
                            exclude_jurisdiction = "/";
                            key_name = "PHILADELPHIA";
                            folder_name = "/PHILADELPHIA";
                    }

                    var jsi = new JurisdictionSummaryItem();
                    jsi.rpt_date = $"{current_date.Month}/{current_date.Day}/{current_date.Year}";
                    jsi.host_name = prefix;

                    result.Add(key_name, jsi);

                    var usr_count = new ItemCount();
                    usr_count.host_name = key_name;
                    usr_count.folder_name = folder_name;
                    user_count_result.Add(key_name, usr_count);

                    var record_count = new ItemCount();
                    record_count.host_name = key_name;
                    record_count.folder_name = folder_name;
                    record_count_result.Add(key_name, record_count);

                    user_count_task_list.Add(GetUserCount(cancellationToken, prefix, config.Value, usr_count, jsi, exclude_jurisdiction));
                    record_count_task_list.Add(GetCaseCount(cancellationToken, prefix, config.Value, record_count, exclude_jurisdiction));
                    //jurisdiction_count_task_list.Add(GetJurisdictions(cancellationToken, prefix, config.Value, jsi));
                }

            }
            else
            {

                var jsi = new JurisdictionSummaryItem();
                jsi.rpt_date = $"{current_date.Month}/{current_date.Day}/{current_date.Year}";
                jsi.host_name = prefix;

                result.Add(prefix, jsi);

                var usr_count = new ItemCount();
                usr_count.host_name = prefix;
                user_count_result.Add(prefix, usr_count);

                var record_count = new ItemCount();
                record_count.host_name = prefix;
                record_count_result.Add(prefix, record_count);

                user_count_task_list.Add(GetUserCount(cancellationToken, prefix, config.Value, usr_count, jsi, exclude_jurisdiction));
                record_count_task_list.Add(GetCaseCount(cancellationToken, prefix, config.Value, record_count, exclude_jurisdiction));
                //jurisdiction_count_task_list.Add(GetJurisdictions(cancellationToken, prefix, config.Value, jsi));
            }
        }


        await Task.WhenAll(user_count_task_list);
        cancellationToken.ThrowIfCancellationRequested();
        await Task.WhenAll(record_count_task_list);
        cancellationToken.ThrowIfCancellationRequested();
        //var user_count_call_results = user_count_responses.Where(r => !string.IsNullOrWhiteSpace(r)); //filter out any null values


        //await Task.WhenAll(jurisdiction_count_task_list);
        //cancellationToken.ThrowIfCancellationRequested();
        //var jurisdiction_count_call_results = jurisdiction_count_responses.Where(r => !string.IsNullOrWhiteSpace(r)); //filter out any null values
        foreach(var kvp in user_count_result)
        {
            result[kvp.Key].num_users_unq = kvp.Value.total;
        }

        foreach(var kvp in record_count_result)
        {
            result[kvp.Key].num_recs = kvp.Value.total;
        }

        List<JurisdictionSummaryItem> view_data = new();

        foreach(var item in result)
        {
            item.Value.host_name = item.Key;
            view_data.Add(item.Value);
        }

        view_data.Sort(new JSIComparer());

        return view_data;
    }

    public async System.Threading.Tasks.Task GetUserCount
    (
        System.Threading.CancellationToken cancellationToken, 
        string p_id, 
        mmria.common.couchdb.DBConfigurationDetail p_config_detail, 
        ItemCount p_result, 
        JurisdictionSummaryItem p_SummaryItem,
        string exclude_jurisdiction
    ) 
    { 
        try
        {
            string request_string = $"{p_config_detail.url}/_users/_all_docs?include_docs=true&skip=1";

            var user_curl = new cURL("GET",null,request_string,null, p_config_detail.user_name, p_config_detail.user_value);
            string responseFromServer = await user_curl.executeAsync();

            var user_alldocs_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<mmria.common.model.couchdb.user>>(responseFromServer);

            HashSet<string> user_id_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<mmria.common.model.couchdb.get_response_item<mmria.common.model.couchdb.user>> temp_list = new List<mmria.common.model.couchdb.get_response_item<mmria.common.model.couchdb.user>>();
            foreach(mmria.common.model.couchdb.get_response_item<mmria.common.model.couchdb.user> uai in user_alldocs_response.rows)
            {
                cancellationToken.ThrowIfCancellationRequested();


                if(string.IsNullOrWhiteSpace(p_config_detail.prefix))
                {
                    if(uai.doc.app_prefix_list == null)
                    {
                        p_result.total +=1; 
                        user_id_set.Add(uai.doc.name);
                    }
                    else if(uai.doc.app_prefix_list.Count == 0 || uai.doc.app_prefix_list.ContainsKey("__no_prefix__"))
                    {
                        p_result.total +=1;
                        user_id_set.Add(uai.doc.name);
                    }
                }
                else if(uai.doc.app_prefix_list.ContainsKey(p_config_detail.prefix.ToLower()))
                {
                    p_result.total +=1;
                    user_id_set.Add(uai.doc.name);
                }
                else
                {

                }
            }

            await GetJurisdictions
            (
                cancellationToken,  
                p_id, 
                p_config_detail, 
                p_SummaryItem, 
                user_id_set,
                exclude_jurisdiction
            );

            p_result.total = user_id_set.Count;

        }
        catch(System.Exception)
        {
            p_result.total = -1;
        }
    }

    public async System.Threading.Tasks.Task GetCaseCount
    (
        System.Threading.CancellationToken cancellationToken, 
        string p_id, 
        mmria.common.couchdb.DBConfigurationDetail p_config_detail, 
        ItemCount p_result,
        string exclude_jurisdiction
    ) 
    { 
        try
        {
            string request_string = $"{p_config_detail.url}/{p_config_detail.prefix}mmrds/_design/sortable/_view/by_jurisdiction_id?skip=0&take=100000";


            cancellationToken.ThrowIfCancellationRequested();
            var user_curl = new cURL("GET",null,request_string,null, p_config_detail.user_name, p_config_detail.user_value);
            string responseFromServer = await user_curl.executeAsync();

            cancellationToken.ThrowIfCancellationRequested();

            var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

            if
            (
                p_result.host_name.ToUpper() == "NY" ||
                p_result.host_name.ToUpper() == "NYC" ||
                p_result.host_name.ToUpper() == "PA" ||
                p_result.host_name.ToUpper() == "PHILADELPHIA"

            )
            {

                var count = 0;
                foreach(var cvr in case_view_response.rows)
                {

                    if(p_result.folder_name == "/")
                    {
                        if
                        (
                            !cvr.value.jurisdiction_id.StartsWith("/PHILADELPHIA", StringComparison.OrdinalIgnoreCase) &&
                            !cvr.value.jurisdiction_id.StartsWith("/NYC", StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            count += 1;
                        }
                    }
                    else if(p_result.folder_name == "/NYC")
                    {
                        if(cvr.value.jurisdiction_id.StartsWith("/NYC", StringComparison.OrdinalIgnoreCase))
                        {
                            count += 1;
                        }
                    }
                    else if(p_result.folder_name == "/PHILADELPHIA")
                    {
                        if(cvr.value.jurisdiction_id.StartsWith("/PHILADELPHIA", StringComparison.OrdinalIgnoreCase))
                        {
                            count += 1;
                        }
                        
                    }
                }

                p_result.total = count;
            }
            else
            {
                p_result.total = case_view_response.total_rows;
            }


            

        }
        catch(System.Exception)
        {
            p_result.total = -1;
        }


    }

    
    public async Task GetJurisdictions
    (
        System.Threading.CancellationToken cancellationToken,  
        string p_id, 
        mmria.common.couchdb.DBConfigurationDetail p_config_detail, 
        JurisdictionSummaryItem p_result, 
        HashSet<string> p_user_id_set,
        string exclude_jurisdiction
    ) 
    {
        //string sort = "by_date_created";
        string search_key = null;
        bool descending = false;
        int skip = 0;
        int take = 20000;
        search_key = "";
        string sort_view = "by_date_created";

        try
        {
            System.Text.StringBuilder request_builder = new System.Text.StringBuilder ();
            request_builder.Append (p_config_detail.url);
            request_builder.Append ($"/{p_config_detail.prefix}jurisdiction/_design/sortable/_view/{sort_view}?");


            if (string.IsNullOrWhiteSpace (search_key))
            {
                if (skip > -1) 
                {
                    request_builder.Append ($"skip={skip}");
                } 
                else 
                {

                    request_builder.Append ("skip=0");
                }


                if (take > -1) 
                {
                    request_builder.Append ($"&limit={take}");
                }

                if (descending) 
                {
                    request_builder.Append ("&descending=true");
                }
            } 
            else 
            {
                request_builder.Append ("skip=0");

                if (descending) 
                {
                    request_builder.Append ("&descending=true");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var user_role_jurisdiction_curl = new cURL("GET", null, request_builder.ToString(), null, p_config_detail.user_name, p_config_detail.user_value);
            string response_from_server = await user_role_jurisdiction_curl.executeAsync ();

            cancellationToken.ThrowIfCancellationRequested();

            var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.user_role_jurisdiction>>(response_from_server);

            HashSet<string> Jurisdictin_User_Set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var Jurisdictin_Role_Dictionary = new Dictionary<string,HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                {  "jurisdiction_admin", new HashSet<string>(StringComparer.OrdinalIgnoreCase)},
                { "abstractor", new HashSet<string>(StringComparer.OrdinalIgnoreCase)},
                { "data_analyst", new HashSet<string>(StringComparer.OrdinalIgnoreCase)},
                { "committee_member", new HashSet<string>(StringComparer.OrdinalIgnoreCase)},
            };



            foreach(mmria.common.model.couchdb.get_sortable_view_response_item<mmria.common.model.couchdb.user_role_jurisdiction> cvi in case_view_response.rows)
            {
                if(string.IsNullOrWhiteSpace(cvi.value.role_name)) continue;

                if(string.IsNullOrWhiteSpace(cvi.value.jurisdiction_id)) continue;

                if(string.IsNullOrWhiteSpace(cvi.value.user_id)) continue;

                if(cvi.value.is_active.HasValue && cvi.value.is_active.Value == false) continue;


                var user_id = cvi.value.user_id;

                if(!p_user_id_set.Contains(user_id)) continue;

                if(!string.IsNullOrWhiteSpace(exclude_jurisdiction))
                {
                    if
                    (
                        exclude_jurisdiction ==
                        cvi.value.jurisdiction_id.ToUpper()
                    )
                    {
                        continue;
                    }
                }

                if(Jurisdictin_Role_Dictionary.ContainsKey(cvi.value.role_name))
                {
                    Jurisdictin_Role_Dictionary[cvi.value.role_name].Add(cvi.value.user_id);
                    Jurisdictin_User_Set.Add(cvi.value.user_id);
                }
            }

            p_result.num_users_ja = Jurisdictin_Role_Dictionary["jurisdiction_admin"].Count;
            p_result.num_users_abs = Jurisdictin_Role_Dictionary["abstractor"].Count;
            p_result.num_user_anl = Jurisdictin_Role_Dictionary["data_analyst"].Count;
            p_result.num_user_cm = Jurisdictin_Role_Dictionary["committee_member"].Count;

            p_user_id_set.RemoveWhere( x=> !Jurisdictin_User_Set.Contains(x));
            
        }
        catch(System.Exception)
        {
            //System.Console.WriteLine (ex);
            p_result.num_users_ja = -1;
            p_result.num_users_abs = -1;
            p_result.num_user_anl = -1;
            p_result.num_user_cm = -1;

        } 
    }
}
