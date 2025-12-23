public sealed class OfflineApplicationUser 
{
    public string OfflineKey { get; set; }

    public OfflineApplicationUser() { }
    public OfflineApplicationUser(string offlineKey) 
    {
        this.OfflineKey = offlineKey;
    }
}