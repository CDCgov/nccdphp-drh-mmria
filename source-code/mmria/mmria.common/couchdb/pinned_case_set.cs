using System;
using System.Collections.Generic;

namespace mmria.common.model.couchdb;

public sealed class pinned_case_set
{
    public string _id { get; } = "pinned-case-set";
    public string _rev { get; set; }
    public DateTime date_created { get; set; } 
    public string created_by { get; set; } 
    public DateTime date_last_updated { get; set; } 
    public string last_updated_by { get; set; } 

    public string data_type { get; } = "pinned_case_set";

    public Dictionary<string,HashSet<string>> list { get; set; } 

    public pinned_case_set()
    {
        this.list = new (StringComparer.OrdinalIgnoreCase);
        
    }

    public bool pin_case(pin_case_message item)
    {
        if
        (
            string.IsNullOrWhiteSpace(item.user_id) ||
            string.IsNullOrWhiteSpace(item.case_id)
        )
        return false;

        if(!this.list.ContainsKey(item.user_id))
            this.list.Add(item.user_id,  new (StringComparer.OrdinalIgnoreCase));
    
        this.list[item.user_id].Add(item.case_id);


        return true;
    }

    public bool unpin_case(pin_case_message item)
    {
        if
        (
            string.IsNullOrWhiteSpace(item.user_id) ||
            string.IsNullOrWhiteSpace(item.case_id)
        )
        return true;

        if(!this.list.ContainsKey(item.user_id))
            return true;
            
    
        this.list[item.user_id].Remove(item.case_id);

        return true;

    }

}

public sealed class pin_case_message
{
    public pin_case_message() { }

    public bool is_pin { get; set; }
    public string case_id { get; set; }
    public string user_id { get; set; }
}

