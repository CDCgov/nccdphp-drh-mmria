using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.OverdoseReport.DAL;
using mmria.common.SharedLibraries.OverdoseReport.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.OverdoseReport.Manager;

public sealed class OverdoseReportManager
{
    private readonly OverdoseReportDAL _dal;

    public OverdoseReportManager(OverdoseReportDAL dal)
    {
        _dal = dal;
    }

    public async Task<OverdoseMeasureResult> GetOverdoseMeasuresAsync(DBConfigurationDetail dbConfig)
    {
        var selector = new ReportSelector
        {
            limit = 10000,
            use_index = "opioid-report-index",
            selector = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["_id"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["$regex"] = "^opioid"
                }
            }
        };

        string selectorJson = SerializeSelector(selector);
        Console.WriteLine(selectorJson);

        var result = await _dal.FindOverdoseMeasuresAsync(selectorJson, dbConfig);
        Console.WriteLine($"case_response.docs.length {result.docs?.Length ?? 0}");
        return result;
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
