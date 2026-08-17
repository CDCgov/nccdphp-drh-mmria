namespace mmria.common.metadata;

public sealed class SystemOfflineConfig
{
    public string _id { get; } = "system-offline-config";
    public string _rev { get; set; }
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
    /// "cdc" is always treated as included regardless of this list.
    /// </summary>
    public System.Collections.Generic.List<string> selected_jurisdictions { get; set; } = new();
    /// <summary>
    /// Expected duration of the maintenance window in hours.
    /// Used by {{outage_duration}} and {{estimated_restoration}} message tokens.
    /// </summary>
    public int restoration_hours { get; set; } = 2;
    /// <summary>
    /// Minutes after the offline modal appears before the user is automatically signed out.
    /// </summary>
    public int auto_logout_minutes { get; set; } = 5;
    public string data_type { get; } = "system_offline_config";
}
