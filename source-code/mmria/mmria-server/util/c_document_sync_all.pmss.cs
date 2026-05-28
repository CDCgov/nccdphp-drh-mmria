#if IS_PMSS_ENHANCED
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mmria.pmss.server.utils;

public sealed class c_document_sync_all
{
/*
{
"index": {
"partial_filter_selector": {
    "_id": {
        "$regex": "^opioid"

    }
},
"fields": ["_id"]
},
"ddoc" : "opioid-report-index",
"type" : "json"
}
*/
    public sealed class Report_Opioid_Index_Attribute_Partial_Filter_Selector
    {
        public Report_Opioid_Index_Attribute_Partial_Filter_Selector(){}
        public Dictionary<string,string> _id
        { get;set;} = new Dictionary<string, string>(){
        {"$regex", "^opioid"}};

    }

    public sealed class Report_PowerBI_Index_Attribute_Partial_Filter_Selector
    {
        public Report_PowerBI_Index_Attribute_Partial_Filter_Selector(){}
        public Dictionary<string,string> _id
        { get;set;} = new Dictionary<string, string>(){
        {"$regex", "^powerbi"}};

    }
    public sealed class Report_Opioid_Index_Attribute_Struct
    {
        public Report_Opioid_Index_Attribute_Struct(){}

        public  Report_Opioid_Index_Attribute_Partial_Filter_Selector
            partial_filter_selector { get; set;} = new Report_Opioid_Index_Attribute_Partial_Filter_Selector();
            public List<string> fields { get; set;} = new List<string>(){"_id"}; 
    }

    public sealed class Report_PowerBI_Index_Attribute_Struct
    {
        public Report_PowerBI_Index_Attribute_Struct(){}

        public  Report_PowerBI_Index_Attribute_Partial_Filter_Selector
            partial_filter_selector { get; set;} = new Report_PowerBI_Index_Attribute_Partial_Filter_Selector();
            public List<string> fields { get; set;} = new List<string>(){"_id"}; 
    }  
    public sealed class Report_Opioid_Index_Struct
    {
        public Report_Opioid_Index_Struct(){}
        public Report_Opioid_Index_Attribute_Struct index {get;set;} = new Report_Opioid_Index_Attribute_Struct();

        public string ddoc { get; set; } = "opioid-report-index";
        public string type {get; set;} = "json";
    }

    public sealed class Report_PowerBI_Index_Struct
    {
        public Report_PowerBI_Index_Struct(){}
        public Report_PowerBI_Index_Attribute_Struct index {get;set;} = new Report_PowerBI_Index_Attribute_Struct();

        public string ddoc { get; set; } = "powerbi-report-index";
        public string type {get; set;} = "json";
    }

    string couchdb_url;
    string user_name;
    string user_value;

    string metadata_version;
    mmria.common.couchdb.DBConfigurationDetail db_config = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public c_document_sync_all 
    (
        string p_couchdb_url, 
        string p_user_name, 
        string p_value,
        string p_metadata_version,
        mmria.common.couchdb.DBConfigurationDetail _db_config,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        this.couchdb_url = p_couchdb_url;
        this.user_name = p_user_name;
        this.user_value = p_value;

        metadata_version = p_metadata_version;
        db_config = _db_config;
        _couchDbHttpClient = couchDbHttpClient;
    }


    public async Task executeAsync ()
    {
        try
        {

            await _couchDbHttpClient.ExecuteAsync("DELETE", this.couchdb_url + $"/{db_config.prefix}de_id", null, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }
        

        try
        {
            await _couchDbHttpClient.ExecuteAsync("DELETE", this.couchdb_url + $"/{db_config.prefix}report", null, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }


        try
        {
            await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{db_config.prefix}de_id", null, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }

        try 
        {
            
            string current_directory = AppContext.BaseDirectory;
            if(!System.IO.Directory.Exists(System.IO.Path.Combine(current_directory, "database-scripts")))
            {
                current_directory = System.IO.Directory.GetCurrentDirectory();
            }

            using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine( current_directory,  "database-scripts/case_design_sortable.json")))
            {
                string result = await sr.ReadToEndAsync ();
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{db_config.prefix}de_id/_design/sortable", result, this.user_name, this.user_value);					
            }


        } 
        catch (Exception) 
        {

        }



        try
        {
            await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{db_config.prefix}report", null, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }


        try
        {
            var Report_Opioid_Index = new Report_Opioid_Index_Struct();
            string index_json = Newtonsoft.Json.JsonConvert.SerializeObject (Report_Opioid_Index);
            await _couchDbHttpClient.ExecuteAsync("POST", this.couchdb_url + $"/{db_config.prefix}report/_index", index_json, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }

        try
        {
            var Report_PowerBI_Index = new Report_PowerBI_Index_Struct();
            
            string index_json = Newtonsoft.Json.JsonConvert.SerializeObject (Report_PowerBI_Index);
            await _couchDbHttpClient.ExecuteAsync("POST", this.couchdb_url + $"/{db_config.prefix}report/_index", index_json, this.user_name, this.user_value);
        }
        catch (Exception)
        {
        
        }

        try
        {
            string current_directory = AppContext.BaseDirectory;
            if(!System.IO.Directory.Exists(System.IO.Path.Combine(current_directory, "database-scripts")))
            {
                current_directory = System.IO.Directory.GetCurrentDirectory();
            }

            using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine( current_directory,  "database-scripts/interactive-aggregate-report-view.json")))
            {
                string result = await sr.ReadToEndAsync ();
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{db_config.prefix}report/_design/interactive_aggregate_report", result, this.user_name, this.user_value);					
            }

        }
        catch (Exception)
        {
        
        }

        try
        {
            string current_directory = AppContext.BaseDirectory;
            if(!System.IO.Directory.Exists(System.IO.Path.Combine(current_directory, "database-scripts")))
            {
                current_directory = System.IO.Directory.GetCurrentDirectory();
            }

            using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine( current_directory,  "database-scripts/data-summary-view.json")))
            {
                string result = await sr.ReadToEndAsync ();
                await _couchDbHttpClient.ExecuteAsync("PUT", this.couchdb_url + $"/{db_config.prefix}report/_design/data_summary_view_report", result, this.user_name, this.user_value);
            }

        }
        catch (Exception)
        {
        
        }

        var page = 0;
        const int page_size = 100;
        var result_count = int.MaxValue;

        while(result_count >= 1)
        try
        {
            string res = await _couchDbHttpClient.ExecuteAsync("GET", this.couchdb_url + $"/{db_config.prefix}mmrds/_all_docs?skip={page}&limit={page_size}", null, this.user_name, this.user_value);
            
            var case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response> (res);

            result_count = case_view_response.rows.Count;

            foreach (mmria.common.model.couchdb.case_view_item cvi in case_view_response.rows)
            {

                try
                {
                    var document_id = cvi.id;

                    if (document_id.IndexOf ("_design/") < 0)
                    {

                        string document_json = await _couchDbHttpClient.ExecuteAsync("GET", this.couchdb_url + $"/{db_config.prefix}mmrds/{document_id}", null, this.user_name, this.user_value);

                        mmria.pmss.server.utils.c_sync_document sync_document = new c_sync_document (document_id, document_json, "PUT", metadata_version, db_config, _couchDbHttpClient);
                        await sync_document.executeAsync ();
                    }

                    
                }
                catch (Exception document_ex)
                {
                    System.Console.Write($"error running c_docment_sync_all.document\n{document_ex}");
                }
                
            }

            page += 1;
        }
        catch (Exception ex)
        {
            System.Console.Write($"error running c_docment_sync_all\n{ex}");

            result_count = 0;
        }

    }
}

#endif