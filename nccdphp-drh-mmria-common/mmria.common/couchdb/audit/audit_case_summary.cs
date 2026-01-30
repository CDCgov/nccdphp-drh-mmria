using System;

namespace mmria.common.model.couchdb.audit;


public sealed class Audit_Case_Summary
{

    public sealed class Audit_Case_Summary_Item
    {
        public Audit_Case_Summary_Item(){}

        public string _rev {get;set;}

        public string user_name {get;set;}
        public int? sequence {get; set; }

        public int? temp_index {get; set; }
        public DateTime? date_created {get;set;}
        public string object_path {get;set;}
        public string metadata_path {get;set;}
        public string old_value {get;set;}
        public string new_value {get;set;}
        public string dictionary_path {get;set;}

        public int? form_index {get;set;}
        public int? grid_index {get;set;}
        public string prompt {get;set;}

        public string metadata_version {get;set;}
        
        public string metadata_type {get;set;}

        public string doc_type {get; set; } = "Audit_Case_Summary_Item";
    }

    public Audit_Case_Summary()
    {
        this.items = new();
    }

    public string doc_type { get; set; } = "Audit_Case_Summary";
    public string _id { get;set; } // ": "245800f9-9528-4777-89ff-1c1d27ddd05f",
    public string _rev { get;set; } // ": "1-aa412d69475e8e489049276758b6469a",
    public string case_id { get;set; } // ": "006b515c-8d43-4471-8f9e-72c4db60bc58",
    public string case_rev { get;set; } // ": "7-dcb0eadbf8ea69a47cc1e25cde706ed8",
    
    public string record_id { get;set; }
    public bool? is_delete { get;set; }
    public string delete_rev { get;set; }
    public string first_name { get;set; }
    public string last_name { get;set; }
    
    public string StateDatabase { get;set; }
    public DateTime? date_created { get;set; } // ": "2021-11-16T16:01:24.731Z",
    public string created_by { get;set; } // ": "user9",
    public System.Collections.Generic.List<Audit_Case_Summary_Item> items { get;set; } // ": [


/*

last_name, first_name
MMRIA Record ID: 
Date Created: 
Created By: 
Date Last Updated:
Last Updated By:



detail:
    #
    Update Date/Time
    Elaspe Time since Last Update
    Update By
    Update Action
    MMRIA Field Prompt
    MMRIA Field Path
    Old Value
    New Value	
    metadata_version

*/


}