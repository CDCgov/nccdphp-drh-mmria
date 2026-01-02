using System;
using System.Collections.Generic;

namespace mmria.common.model.couchdb;

public sealed class jurisdiction
{
    public string id { get; set; }
    public string name { get; set; }
    public DateTime date_created { get; set; } 
    public string created_by { get; set; } 
    public DateTime date_last_updated { get; set; } 
    public string last_updated_by { get; set; } 
    public bool is_active { get; set; } 
    public bool is_enabled { get; set; } 

    public List<jurisdiction> children { get; set; } 

    public string parent_id { get; set; }

    public jurisdiction()
    {
        this.children = new List<jurisdiction>();
    }

}


