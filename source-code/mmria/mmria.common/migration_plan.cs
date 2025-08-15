using System;
using System.Collections.Generic;

namespace mmria.common.model;


public sealed class migration_plan_item
{
    public string old_mmria_path { get; set; }
    public string new_mmria_path{ get; set; }
    public string old_value{ get; set; }
    public string new_value{ get; set; }
    public string comment{ get; set; }

}


public sealed class migration_plan 
{

    public migration_plan()
    {
        this.plan_items = new List<migration_plan_item>();
    }
    public string name{ get; set; }
    public string description{ get; set; }
    public DateTime? date_created{ get; set; }
    public string created_by{ get; set; }
    public DateTime? date_last_updated{ get; set; }
    public string last_updated_by{ get; set; }
    public List<migration_plan_item> plan_items{ get; set; }
}
