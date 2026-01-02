using System;

namespace mmria.server.model;

public sealed class LogEntry
{
    public string _id { get; set; }
    public string _rev { get; set; }
    public string data_type { get; set; } = "log_entry";
    public DateTime timestamp { get; set; }
    public string level { get; set; }
    public string context { get; set; }
    public string message { get; set; }
    public string fileName { get; set; }
    public int? lineNumber { get; set; }
    public int? columnNumber { get; set; }
    public string functionName { get; set; }
    public string stackTrace { get; set; }
    public string errorType { get; set; }
    public string is_offline { get; set; }
    public string process_offline_cases { get; set; }
    public string offline_session_id { get; set; }
    public string user_name { get; set; }
    public DateTime date_created { get; set; }
}

public sealed class LogEntryBatch
{
    public LogEntry[] logs { get; set; }
}
