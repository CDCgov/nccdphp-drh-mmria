using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.MMRIARebuild.Model;
using mmria.common.SharedLibraries.PowerBI.DAL;
using mmria.common.SharedLibraries.PowerBI.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.PowerBI.Manager;

public sealed class PowerBIManager
{
    private readonly PowerBIDAL _dal;

    public PowerBIManager(PowerBIDAL dal)
    {
        _dal = dal;
    }

    public async Task<PowerBIMeasureResult> GetPowerBIMeasuresAsync(string indicatorId, DBConfigurationDetail dbConfig)
    {
        var selector = new ReportSelector
        {
            limit = 10000,
            use_index = "powerbi-report-index",
            selector = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["_id"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["$regex"] = "^powerbi"
                }
            }
        };

        var result = await _dal.FindPowerBIMeasuresAsync(SerializeSelector(selector), dbConfig);
        result.docs ??= Array.Empty<c_opioid_report_object>();

        if (string.IsNullOrWhiteSpace(indicatorId))
        {
            return result;
        }

        foreach (var doc in result.docs)
        {
            doc.data = (doc.data ?? new List<opioid_report_value_struct>())
                .Where(item => item.indicator_id == indicatorId && item.value > 0)
                .ToList();
        }

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
