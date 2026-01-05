using System;

namespace mmria.common.model.couchdb.audit;


public sealed class Audit_Detail_View
{
    public Audit_Detail_View()
    {
        this.items = new();
    }
    public string doc_type { get;set; } = "Audit_Detail_View";
    public string _id { get;set; } // ": "245800f9-9528-4777-89ff-1c1d27ddd05f",
    public string _rev { get;set; } // ": "1-aa412d69475e8e489049276758b6469a",
    public string case_id { get;set; } // ": "006b515c-8d43-4471-8f9e-72c4db60bc58",
    public string case_rev { get;set; } // ": "7-dcb0eadbf8ea69a47cc1e25cde706ed8",
    
    public string record_id { get;set; }
    public bool? is_delete { get;set; }
    public string delete_rev { get;set; }
    public string first_name { get;set; }
    public string last_name { get;set; }
    public string user_name { get;set; } // ": "user9",
    public string note { get;set; } // ": "save_and_finish_click",
    public string metadata_version { get;set; } // ": "21.10.01",

    public string StateDatabase { get;set; }
    public DateTime? date_created { get;set; } // ": "2021-11-16T16:01:24.731Z",
    public System.Collections.Generic.List<mmria.common.model.couchdb.Change_Stack> items { get;set; } // ": [


}




