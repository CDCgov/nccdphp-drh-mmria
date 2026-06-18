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
    public string data_type { get; } = "system_offline_config";
}
