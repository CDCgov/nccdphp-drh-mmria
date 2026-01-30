using System;

namespace mmria.common.model.couchdb;

/*
public sealed class AuditViewResponseItem
{
    public string id {get;set;}
    public string rev {get;set;}
    public string user_name {get;set;}
    public DateTime? date_created {get;set;}
    public string note {get;set;}
    public int? change_count {get;set;}
}
*/

public sealed class Change_Stack_Item
{
    public Change_Stack_Item(){}

    public string _id {get;set;}
    public string _rev {get;set;}

    public string user_name {get;set;}

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
    
    public string metadata_type {get;set;}

    public string doc_type {get; set;} = "Change_Stack_Item";
}
public sealed class Change_Stack
{
    public Change_Stack(){}

    public string doc_type {get; set;} = "Change_Stack";
    public string _id {get;set;}
    public string _rev {get;set;}

    public string case_id {get;set;}
    public string case_rev {get;set;}

    public string record_id { get;set; }
    public bool? is_delete { get;set; }
    public string delete_rev { get;set; }

    public string first_name { get;set; }
    public string last_name { get;set; }

    public string user_name {get;set;}

    public string note {get;set;}

    public string metadata_version {get;set;}

    public DateTime? date_created {get;set;}

    public System.Collections.Generic.List<Change_Stack_Item> items {get;set;} = new();
}

public sealed class Save_Case_Request
{
    public Change_Stack Change_Stack {get;set;} = new();

    public System.Dynamic.ExpandoObject Case_Data {get;set;}
    public Save_Case_Request()
    {

    }
}


public sealed class Save_Case_Request <T>
{
    public Change_Stack Change_Stack {get;set;} = new();

    public T Case_Data {get;set;}
    public Save_Case_Request()
    {

    }
}
