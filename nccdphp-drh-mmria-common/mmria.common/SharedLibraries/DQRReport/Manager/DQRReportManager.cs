using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.DQRReport.DAL;
using mmria.common.SharedLibraries.DQRReport.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.DQRReport.Manager;

public sealed class DQRReportManager
{
    private readonly DQRReportDAL _dal;

    public DQRReportManager(DQRReportDAL dal)
    {
        _dal = dal;
    }

    public async Task<DQRReportResult> GetDqrDetailsAsync(string quarterString, DBConfigurationDetail dbConfig)
    {
        var selector = new ReportSelector
        {
            limit = 10000,
            selector = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["data_type"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["$eq"] = "dqr-detail"
                }
            }
        };

        return await _dal.FindDqrDetailsAsync(SerializeSelector(selector), dbConfig);
    }

    private static string SerializeSelector(ReportSelector selector)
    {
        return JsonConvert.SerializeObject(selector, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
    }

    private sealed class ReportSelector
    {
        public Dictionary<string, Dictionary<string, string>> selector { get; set; }
        public string[] fields { get; set; }
        public string use_index { get; set; }
        public int limit { get; set; }
    }
}
