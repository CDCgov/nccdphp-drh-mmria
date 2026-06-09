namespace mmria.common.SharedLibraries.Logging.Model;

public sealed class LoggingLogQuery
{
    public string level { get; set; }
    public string context { get; set; }
    public string sessionId { get; set; }
    public string userName { get; set; }
    public string search { get; set; }
    public string startDate { get; set; }
    public string endDate { get; set; }
    public int skip { get; set; }
}
