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
    public string data_type { get; } = "system_offline_config";
}
