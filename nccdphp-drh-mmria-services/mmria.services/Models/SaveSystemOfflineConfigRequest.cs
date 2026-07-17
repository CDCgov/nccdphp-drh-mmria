using System.Collections.Generic;

namespace mmria.services.Models;

/// <summary>
/// Input model for the SaveSystemOfflineConfig endpoint.
/// Only contains fields that clients are permitted to supply.
/// Server-managed fields (_id, _rev, data_type) are intentionally excluded
/// to prevent mass assignment of document-identity and revision properties.
/// </summary>
public sealed class SaveSystemOfflineConfigRequest
{
    public string warn_date { get; set; }
    public string warn_message { get; set; }
    public string offline_date { get; set; }
    public string offline_modal_message { get; set; }
    public string offline_page_message { get; set; }
    public bool apply_to_all_jurisdictions { get; set; } = true;
    public List<string> selected_jurisdictions { get; set; } = new();
    public int restoration_hours { get; set; } = 2;
    public int auto_logout_minutes { get; set; } = 5;
}
