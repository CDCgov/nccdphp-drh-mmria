using System;

namespace mmria.common.model.couchdb.audit;



public sealed class Audit_Manage_User
{
    public sealed class Audit_Manage_User_Item
    {

        
/*

{
    _id: $mmria.get_new_guid(),
    user_id: p_user_id,
    action: p_action,
    element_id: p_elem_id,
    prev_value: p_prev_val,
    value: p_val,
    date_created: new Date(),
    created_by: g_user_name,
    date_last_updated: new Date(),
    last_updated_by: g_user_name,
    data_id: p_data_id,
    parent_id: '',
    data_type: "audit_history"
}


*/
        public Audit_Manage_User_Item(){}

        public DateTimeOffset? date_created { get; set; }
        public string created_by { get; set; }
        public string action { get; set; }
        public int? element_id { get; set; }
        public string user_id { get; set; }

        public string field { get; set; }
        public string field_path {get;set;}
        public string old_value {get;set;}
        public string new_value {get;set;}
        
        public string note {get;set;}
        
 

  
    }

    public Audit_Manage_User()
    {
        this.items = new();
    }

    public string doc_type { get;set; } = "Audit_Manage_User";
    public string _id { get; set; } = "audit-manage-user";
    public string _rev { get;set; }
    public bool? is_delete { get;set; }
    public string delete_rev { get;set; }

    public DateTimeOffset? date_created { get;set; }
    public System.Collections.Generic.List<Audit_Manage_User_Item> items { get;set; } 


}




