using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonSessionSummaryItem = mmria.common.SharedLibraries.SummaryReport.Model.SessionSummaryItem;
using SummaryReportManager = mmria.common.SharedLibraries.SummaryReport.Manager.SummaryReportManager;

namespace mmria.server.utils;

public sealed class SessionSummaryItem
{
    public SessionSummaryItem()
    {
        rpt_date = new List<int>();
    }

    public string host_name { get; set; }
    public List<int> rpt_date { get; set; }
    public int total { get; set; }
}

public sealed class SessionSummary
{
    private readonly mmria.common.couchdb.ConfigurationSet _configDb;
    private readonly SummaryReportManager _summaryReportManager;

    public SessionSummary(
        mmria.common.couchdb.ConfigurationSet configDb,
        SummaryReportManager summaryReportManager)
    {
        _configDb = configDb;
        _summaryReportManager = summaryReportManager;
    }

    public async Task<List<SessionSummaryItem>> execute(CancellationToken cancellationToken)
    {
        var summary = await _summaryReportManager.GetSessionSummaryAsync(_configDb, cancellationToken);
        return summary.Select(ToServerModel).ToList();
    }

    private static SessionSummaryItem ToServerModel(CommonSessionSummaryItem item)
    {
        return new SessionSummaryItem
        {
            host_name = item.host_name,
            rpt_date = item.rpt_date,
            total = item.total
        };
    }
}
