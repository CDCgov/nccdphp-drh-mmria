using System;
using System.Collections.Generic;
namespace mmria.common.metadata;

public sealed class State_List_Item
{
    public bool? is_included { get; set; }
    public string prefix { get; set; }
    public string name { get; set; }
}
public record Populate_CDC_Instance_Record()
{
    public DateTime? date_submitted { get; init; } = DateTime.Now;

    public DateTime? date_completed { get; init; }

    public int? duration_in_hours { get; init; } = 0;
    public int? duration_in_minutes { get; init; } = 0;

    public string transfer_result { get; init; }
    public int? transfer_status_number { get; init; }
    public string error_message { get; set; }
}


public sealed class Populate_CDC_Instance
{
    public Populate_CDC_Instance() { this.state_list = new List<State_List_Item>(); }
    public string _id { get; set; }
    public string _rev { get; set; }

    public DateTime? date_submitted { get; set; } = DateTime.Now;

    public DateTime? date_completed { get; set; }

    public int? duration_in_hours { get; set; } = 0;
    public int? duration_in_minutes { get; set; } = 0;

   public string transfer_result { get; set; }
  public int? transfer_status_number { get; set; }

  public string error_message { get; set; }
  

    public List<State_List_Item> state_list { get; set; }
}
