using System;
using System.Collections.Generic;

namespace mmria.common.model.couchdb;

public sealed class migration_plan_item
{
    public string old_mmria_path { get; set; }
    public string new_mmria_path { get; set; }
    public string old_value { get; set; }
    public string new_value { get; set; }
    public string comment { get; set; }

    public migration_plan_item(){}
}

public sealed class migration_plan
{
    public string _id { get; set; }
    public string _rev { get; set; }

    public string name { get; set; }
    public string description{ get; set; }
    public DateTime? date_created { get; set; } 
    public string created_by { get; set; } 
    public DateTime? date_last_updated { get; set; } 
    public string last_updated_by { get; set; }

    public string data_type { get; set;} = "migration_plan";
    public List<migration_plan_item> plan_items{ get; set; }

    public migration_plan()
    {
        plan_items = new List<migration_plan_item>();
    }
}



