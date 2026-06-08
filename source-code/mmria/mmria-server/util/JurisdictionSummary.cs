using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonJurisdictionSummaryItem = mmria.common.SharedLibraries.SummaryReport.Model.JurisdictionSummaryItem;
using SummaryReportManager = mmria.common.SharedLibraries.SummaryReport.Manager.SummaryReportManager;

namespace mmria.server.utils;

public sealed class ItemCount
{
    public string host_name { get; set; }
    public string folder_name { get; set; } = "/";
    public int total { get; set; }
}

public sealed class JurisdictionSummaryItem
{
    public string host_name { get; set; }
    public string folder_name { get; set; } = "/";
    public string rpt_date { get; set; }
    public int num_recs { get; set; }
    public int num_users_unq { get; set; }
    public int num_users_ja { get; set; }
    public int num_users_abs { get; set; }
    public int num_user_anl { get; set; }
    public int num_user_cm { get; set; }
}

public sealed class JurisdictionSummary
{
    private readonly mmria.common.couchdb.ConfigurationSet _configDb;
    private readonly SummaryReportManager _summaryReportManager;

    public JurisdictionSummary(
        mmria.common.couchdb.ConfigurationSet configDb,
        SummaryReportManager summaryReportManager)
    {
        _configDb = configDb;
        _summaryReportManager = summaryReportManager;
    }

    public async Task<List<JurisdictionSummaryItem>> execute(CancellationToken cancellationToken)
    {
        var summary = await _summaryReportManager.GetJurisdictionSummaryAsync(_configDb, cancellationToken);
        return summary.Select(ToServerModel).ToList();
    }

    private static JurisdictionSummaryItem ToServerModel(CommonJurisdictionSummaryItem item)
    {
        return new JurisdictionSummaryItem
        {
            host_name = item.host_name,
            folder_name = item.folder_name,
            rpt_date = item.rpt_date,
            num_recs = item.num_recs,
            num_users_unq = item.num_users_unq,
            num_users_ja = item.num_users_ja,
            num_users_abs = item.num_users_abs,
            num_user_anl = item.num_user_anl,
            num_user_cm = item.num_user_cm
        };
    }
}
