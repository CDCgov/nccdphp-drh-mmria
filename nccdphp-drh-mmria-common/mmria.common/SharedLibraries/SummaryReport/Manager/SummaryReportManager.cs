using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.SummaryReport.DAL;
using mmria.common.SharedLibraries.SummaryReport.Model;

namespace mmria.common.SharedLibraries.SummaryReport.Manager;

public sealed class SummaryReportManager
{
    private const int MaxDegreeOfParallelism = 6;
    private const int SessionLookbackDays = 30;
    private const int RecentSessionLimit = 500;
    private readonly SummaryReportDAL _dal;

    public SummaryReportManager(SummaryReportDAL dal)
    {
        _dal = dal;
    }

    public async Task<List<JurisdictionSummaryItem>> GetJurisdictionSummaryAsync(
        ConfigurationSet configDb,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, JurisdictionSummaryItem>(StringComparer.OrdinalIgnoreCase);
        var userCountResult = new Dictionary<string, ItemCount>(StringComparer.OrdinalIgnoreCase);
        var recordCountResult = new Dictionary<string, ItemCount>(StringComparer.OrdinalIgnoreCase);
        var userCountTasks = new List<Func<Task>>();
        var recordCountTasks = new List<Func<Task>>();
        var currentDate = DateTime.Now;

        foreach (var config in configDb.detail_list)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var prefix = config.Key.ToUpperInvariant();
            if (prefix == "VITAL_IMPORT")
            {
                continue;
            }

            if (prefix == "NY" || prefix == "PA")
            {
                var excludedCityJurisdiction = prefix == "NY" ? "/NYC" : "/PHILADELPHIA";
                AddJurisdictionSummaryItem(prefix, prefix, "/", excludedCityJurisdiction, config.Value);

                var cityKey = prefix == "NY" ? "NYC" : "PHILADELPHIA";
                var cityFolder = prefix == "NY" ? "/NYC" : "/PHILADELPHIA";
                AddJurisdictionSummaryItem(cityKey, prefix, cityFolder, "/", config.Value);
            }
            else
            {
                AddJurisdictionSummaryItem(prefix, prefix, "/", string.Empty, config.Value);
            }
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(userCountTasks, parallelOptions, async (factory, ct) => await factory());
        cancellationToken.ThrowIfCancellationRequested();
        await Parallel.ForEachAsync(recordCountTasks, parallelOptions, async (factory, ct) => await factory());
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var kvp in userCountResult)
        {
            result[kvp.Key].num_users_unq = kvp.Value.total;
        }

        foreach (var kvp in recordCountResult)
        {
            result[kvp.Key].num_recs = kvp.Value.total;
        }

        var viewData = new List<JurisdictionSummaryItem>();
        foreach (var item in result)
        {
            item.Value.host_name = item.Key;
            viewData.Add(item.Value);
        }

        viewData.Sort(JurisdictionSummaryComparer.Instance);
        return viewData;

        void AddJurisdictionSummaryItem(
            string key,
            string hostName,
            string folderName,
            string excludeJurisdiction,
            DBConfigurationDetail dbConfig)
        {
            var summaryItem = new JurisdictionSummaryItem
            {
                rpt_date = $"{currentDate.Month}/{currentDate.Day}/{currentDate.Year}",
                host_name = hostName
            };
            result.Add(key, summaryItem);

            var userCount = new ItemCount
            {
                host_name = key,
                folder_name = folderName
            };
            userCountResult.Add(key, userCount);

            var recordCount = new ItemCount
            {
                host_name = key,
                folder_name = folderName
            };
            recordCountResult.Add(key, recordCount);

            userCountTasks.Add(() => PopulateUserCountAsync(
                cancellationToken,
                dbConfig,
                userCount,
                summaryItem,
                excludeJurisdiction));

            recordCountTasks.Add(() => PopulateCaseCountAsync(
                cancellationToken,
                dbConfig,
                recordCount));
        }
    }

    public async Task<List<SessionSummaryItem>> GetSessionSummaryAsync(
        ConfigurationSet configDb,
        CancellationToken cancellationToken)
    {
        var result = new List<SessionSummaryItem>();
        var recordCountResult = new Dictionary<string, SessionSummaryItem>(StringComparer.OrdinalIgnoreCase);
        var recordCountTasks = new List<Func<Task>>();

        foreach (var config in configDb.detail_list)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var prefix = config.Key.ToUpperInvariant();
            if (prefix == "VITAL_IMPORT")
            {
                continue;
            }

            var sessionSummaryItem = new SessionSummaryItem
            {
                host_name = prefix
            };
            recordCountResult.Add(prefix, sessionSummaryItem);
            recordCountTasks.Add(() => PopulateSessionCountAsync(cancellationToken, config.Value, sessionSummaryItem));
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(recordCountTasks, parallelOptions, async (factory, ct) => await factory());
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var kvp in recordCountResult)
        {
            result.Add(kvp.Value);
        }

        result.Sort(SessionSummaryComparer.Instance);
        return result;
    }

    private async Task PopulateUserCountAsync(
        CancellationToken cancellationToken,
        DBConfigurationDetail dbConfig,
        ItemCount result,
        JurisdictionSummaryItem summaryItem,
        string excludeJurisdiction)
    {
        try
        {
            var userAllDocsResponse = await _dal.GetUsersAsync(dbConfig);
            var userIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var userItem in userAllDocsResponse.rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var user = userItem.doc;
                if (user == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dbConfig.prefix))
                {
                    if (user.app_prefix_list == null ||
                        user.app_prefix_list.Count == 0 ||
                        user.app_prefix_list.ContainsKey("__no_prefix__"))
                    {
                        result.total += 1;
                        userIdSet.Add(user.name);
                    }
                }
                else if (user.app_prefix_list != null && user.app_prefix_list.ContainsKey(dbConfig.prefix.ToLowerInvariant()))
                {
                    result.total += 1;
                    userIdSet.Add(user.name);
                }
            }

            await PopulateJurisdictionRoleCountsAsync(
                cancellationToken,
                dbConfig,
                summaryItem,
                userIdSet,
                excludeJurisdiction);

            result.total = userIdSet.Count;
        }
        catch (Exception)
        {
            result.total = -1;
        }
    }

    private async Task PopulateCaseCountAsync(
        CancellationToken cancellationToken,
        DBConfigurationDetail dbConfig,
        ItemCount result)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var caseViewResponse = await _dal.GetCaseJurisdictionViewAsync(dbConfig);
            cancellationToken.ThrowIfCancellationRequested();

            if (IsSplitJurisdiction(result.host_name))
            {
                var count = 0;
                foreach (var row in caseViewResponse.rows)
                {
                    var jurisdictionId = row.value?.jurisdiction_id;
                    if (string.IsNullOrWhiteSpace(jurisdictionId))
                    {
                        continue;
                    }

                    if (result.folder_name == "/")
                    {
                        if (!jurisdictionId.StartsWith("/PHILADELPHIA", StringComparison.OrdinalIgnoreCase) &&
                            !jurisdictionId.StartsWith("/NYC", StringComparison.OrdinalIgnoreCase))
                        {
                            count += 1;
                        }
                    }
                    else if (result.folder_name == "/NYC" &&
                             jurisdictionId.StartsWith("/NYC", StringComparison.OrdinalIgnoreCase))
                    {
                        count += 1;
                    }
                    else if (result.folder_name == "/PHILADELPHIA" &&
                             jurisdictionId.StartsWith("/PHILADELPHIA", StringComparison.OrdinalIgnoreCase))
                    {
                        count += 1;
                    }
                }

                result.total = count;
            }
            else
            {
                result.total = caseViewResponse.total_rows;
            }
        }
        catch (Exception)
        {
            result.total = -1;
        }
    }

    private async Task PopulateJurisdictionRoleCountsAsync(
        CancellationToken cancellationToken,
        DBConfigurationDetail dbConfig,
        JurisdictionSummaryItem result,
        HashSet<string> userIdSet,
        string excludeJurisdiction)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var jurisdictionResponse = await _dal.GetUserRoleJurisdictionsAsync(dbConfig);
            cancellationToken.ThrowIfCancellationRequested();

            var jurisdictionUserSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var jurisdictionRoleDictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "jurisdiction_admin", new HashSet<string>(StringComparer.OrdinalIgnoreCase) },
                { "abstractor", new HashSet<string>(StringComparer.OrdinalIgnoreCase) },
                { "data_analyst", new HashSet<string>(StringComparer.OrdinalIgnoreCase) },
                { "committee_member", new HashSet<string>(StringComparer.OrdinalIgnoreCase) }
            };

            foreach (var row in jurisdictionResponse.rows)
            {
                var value = row.value;
                if (value == null ||
                    string.IsNullOrWhiteSpace(value.role_name) ||
                    string.IsNullOrWhiteSpace(value.jurisdiction_id) ||
                    string.IsNullOrWhiteSpace(value.user_id) ||
                    value.is_active == false)
                {
                    continue;
                }

                if (!userIdSet.Contains(value.user_id))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(excludeJurisdiction) &&
                    excludeJurisdiction == value.jurisdiction_id.ToUpperInvariant())
                {
                    continue;
                }

                if (jurisdictionRoleDictionary.ContainsKey(value.role_name))
                {
                    jurisdictionRoleDictionary[value.role_name].Add(value.user_id);
                    jurisdictionUserSet.Add(value.user_id);
                }
            }

            result.num_users_ja = jurisdictionRoleDictionary["jurisdiction_admin"].Count;
            result.num_users_abs = jurisdictionRoleDictionary["abstractor"].Count;
            result.num_user_anl = jurisdictionRoleDictionary["data_analyst"].Count;
            result.num_user_cm = jurisdictionRoleDictionary["committee_member"].Count;

            userIdSet.RemoveWhere(userId => !jurisdictionUserSet.Contains(userId));
        }
        catch (Exception)
        {
            result.num_users_ja = -1;
            result.num_users_abs = -1;
            result.num_user_anl = -1;
            result.num_user_cm = -1;
        }
    }

    private async Task PopulateSessionCountAsync(
        CancellationToken cancellationToken,
        DBConfigurationDetail dbConfig,
        SessionSummaryItem result)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sessionResponse = await _dal.GetRecentSessionsAsync(dbConfig, RecentSessionLimit);
            cancellationToken.ThrowIfCancellationRequested();

            var cutOffDate = DateTime.Now.AddDays(-SessionLookbackDays);
            for (var i = 0; i < SessionLookbackDays; i++)
            {
                result.rpt_date.Add(0);
            }

            for (var i = 0; i < sessionResponse.rows.Count; i++)
            {
                var row = sessionResponse.rows[i];
                if (row.value.date_created >= cutOffDate)
                {
                    var diff = DateTime.Now - row.value.date_created;
                    var rowIndex = diff.Days;
                    result.rpt_date[rowIndex]++;
                }
                else
                {
                    break;
                }
            }
        }
        catch (Exception)
        {
            result.total = -1;
        }
    }

    private static bool IsSplitJurisdiction(string hostName)
    {
        return string.Equals(hostName, "NY", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(hostName, "NYC", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(hostName, "PA", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(hostName, "PHILADELPHIA", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ItemCount
    {
        public string host_name { get; set; }
        public string folder_name { get; set; } = "/";
        public int total { get; set; }
    }

    private sealed class JurisdictionSummaryComparer : IComparer<JurisdictionSummaryItem>
    {
        public static readonly JurisdictionSummaryComparer Instance = new();

        public int Compare(JurisdictionSummaryItem x, JurisdictionSummaryItem y)
        {
            if (x?.host_name == null || y?.host_name == null)
            {
                return 0;
            }

            return x.host_name != y.host_name
                ? string.Compare(x.host_name, y.host_name, StringComparison.Ordinal)
                : string.Compare(x.folder_name, y.folder_name, StringComparison.Ordinal);
        }
    }

    private sealed class SessionSummaryComparer : IComparer<SessionSummaryItem>
    {
        public static readonly SessionSummaryComparer Instance = new();

        public int Compare(SessionSummaryItem x, SessionSummaryItem y)
        {
            if (x?.host_name == null || y?.host_name == null)
            {
                return 0;
            }

            return string.Compare(x.host_name, y.host_name, StringComparison.Ordinal);
        }
    }
}
