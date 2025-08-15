using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mmria.server.utils;

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

    common.couchdb.DBConfigurationDetail connection;

    string metadata_release_version_name;
    private string couchdb_url;
    private string user_name;
    private string user_value;

    private string prefix;

    public c_document_sync_all (common.couchdb.DBConfigurationDetail p_connection, string p_metadata_release_version_name)
    {
        this.connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
        this.couchdb_url = connection.url;
        this.user_name = connection.user_name;
        this.user_value = connection.user_value;
        this.prefix = connection.prefix;
    }


    public async Task executeAsync ()
    {
        try
        {

            var delete_de_id_curl = new mmria.getset.cURL ("DELETE", null, this.couchdb_url + $"/{this.prefix}de_id", null, this.user_name, this.user_value);
            await delete_de_id_curl.executeAsync ();
        }
        catch (Exception)
        {
        
        }
        

        try
        {
            var delete_report_curl = new mmria.getset.cURL ("DELETE", null, this.couchdb_url + $"/{this.prefix}report", null, this.user_name, this.user_value);
            await delete_report_curl.executeAsync ();
        }
        catch (Exception)
        {
        
        }


        try
        {
            var create_de_id_curl = new mmria.getset.cURL ("PUT", null, this.couchdb_url + $"/{this.prefix}de_id", null, this.user_name, this.user_value);
            await create_de_id_curl.executeAsync ();
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
                var create_de_id_curl = new mmria.getset.cURL ("PUT", null, this.couchdb_url + $"/{this.prefix}de_id/_design/sortable", result, this.user_name, this.user_value);
                await create_de_id_curl.executeAsync ();					
            }


        } 
        catch (Exception) 
        {

        }



        try
        {
            var create_report_curl = new mmria.getset.cURL ("PUT", null, this.couchdb_url + $"/{this.prefix}report", null, this.user_name, this.user_value);
            await create_report_curl.executeAsync ();	
        }
        catch (Exception)
        {
        
        }


        try
        {
            var Report_Opioid_Index = new Report_Opioid_Index_Struct();
            string index_json = Newtonsoft.Json.JsonConvert.SerializeObject (Report_Opioid_Index);
            var create_report_index_curl = new mmria.getset.cURL ("POST", null, this.couchdb_url + $"/{this.prefix}report/_index", index_json, this.user_name, this.user_value);
            await create_report_index_curl.executeAsync ();
        }
        catch (Exception)
        {
        
        }

        try
        {
            var Report_PowerBI_Index = new Report_PowerBI_Index_Struct();
            
            string index_json = Newtonsoft.Json.JsonConvert.SerializeObject (Report_PowerBI_Index);
            var create_report_index_curl = new mmria.getset.cURL ("POST", null, this.couchdb_url + $"/{this.prefix}report/_index", index_json, this.user_name, this.user_value);
            await create_report_index_curl.executeAsync ();
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
                var create_de_id_curl = new mmria.getset.cURL ("PUT", null, this.couchdb_url + $"/{this.prefix}report/_design/interactive_aggregate_report", result, this.user_name, this.user_value);
                await create_de_id_curl.executeAsync ();					
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
                var create_de_id_curl = new mmria.getset.cURL ("PUT", null, this.couchdb_url + $"/{this.prefix}report/_design/data_summary_view_report", result, this.user_name, this.user_value);
                await create_de_id_curl.executeAsync ();					
            }

        }
        catch (Exception)
        {
        
        }

        var curl = new mmria.getset.cURL ("GET", null, this.couchdb_url + $"/{this.prefix}mmrds/_all_docs?include_docs=true", null, this.user_name, this.user_value);
        string res = await curl.executeAsync ();
/*
{
"total_rows": 3, "offset": 0, "rows": [
{"id": "doc1", "key": "doc1", "value": {"rev": "4324BB"}},
{"id": "doc2", "key": "doc2", "value": {"rev":"2441HF"}},
{"id": "doc3", "key": "doc3", "value": {"rev":"74EC24"}}
]
}
*/			
        System.Dynamic.ExpandoObject all_docs = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (res);
        try
        {
            IDictionary<string,object> all_docs_dictionary = all_docs as IDictionary<string,object>;
            List<object> row_list = null;
            
            if
            (
                all_docs_dictionary != null &&
                all_docs_dictionary.ContainsKey("rows")
            )
            {
                row_list = all_docs_dictionary ["rows"] as List<object>;	
            }
            
            
            if(row_list != null)
            foreach (object row_item in row_list) 
            {

                try
                {
                    IDictionary<string, object> row_dictionary = row_item as IDictionary<string, object>;
                    if(row_dictionary != null)
                    {
                        IDictionary<string, object> doc_dictionary = row_dictionary ["doc"] as IDictionary<string, object>;
                        if(row_dictionary != null && doc_dictionary != null)
                        {
                            string document_id = doc_dictionary ["_id"].ToString ();
                            if (document_id.IndexOf ("_design/") < 0)
                            {
                                string document_json = Newtonsoft.Json.JsonConvert.SerializeObject (doc_dictionary);
                                mmria.server.utils.c_sync_document sync_document = new c_sync_document (document_id, document_json, connection, metadata_release_version_name);
                                await sync_document.executeAsync ();
                            }
                        }
                    }
                }
                catch (Exception document_ex)
                {
                    System.Console.Write($"error running c_docment_sync_all.document\n{document_ex}");
                }
                
            }
        }
        catch (Exception ex)
        {
            System.Console.Write($"error running c_docment_sync_all\n{ex}");
        }

    }
}

