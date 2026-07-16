using System.Collections.Generic;

namespace mmria.services.Models;

/// <summary>
/// Request DTO for the SaveSystemOfflineConfig endpoint.
/// Only contains fields that a client is permitted to supply; server-owned
/// fields (_id, _rev, data_type) are intentionally excluded to prevent
/// mass assignment (CWE-915).
/// </summary>
public sealed class SaveSystemOfflineConfigRequest
{
    public string warn_date { get; set; }
    public string warn_message { get; set; }
    public string offline_date { get; set; }
    public string offline_modal_message { get; set; }
    public string offline_page_message { get; set; }
    /// <summary>
    /// When true (default), the offline window applies to all jurisdictions.
    /// When false, only tenants listed in <see cref="selected_jurisdictions"/> are affected.
    /// </summary>
    public bool apply_to_all_jurisdictions { get; set; } = true;
    /// <summary>
    /// Tenant/jurisdiction host-prefix values that should receive the offline window.
    /// Only evaluated when <see cref="apply_to_all_jurisdictions"/> is false.
    /// </summary>
    public List<string> selected_jurisdictions { get; set; } = new();
    /// <summary>
    /// Expected duration of the maintenance window in hours.
    /// </summary>
    public int restoration_hours { get; set; } = 2;
    /// <summary>
    /// Minutes after the offline modal appears before the user is automatically signed out.
    /// </summary>
    public int auto_logout_minutes { get; set; } = 5;
}
