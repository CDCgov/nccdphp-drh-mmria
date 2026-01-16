namespace mmria.server.SharedLibraries.Model.OfflineCase;

public class CacheVersionResponse
{
    public string cacheVersion { get; set; }
    public string baseVersion { get; set; }
    public string version { get; set; }
    public string stability { get; set; }
    public string timestamp { get; set; }
}