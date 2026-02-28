namespace mmria.common.SharedLibraries.OfflineCase.Model;

public sealed class LightweightOfflineCaseItem
{
    public LightweightOfflineCaseItem(){}

    public string id { get; set; }
    public string key { get; set; }
    public LightweightOfflineCaseResponse value {  get; set; }

}
