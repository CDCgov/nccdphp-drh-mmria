using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Logging.DAL;
using mmria.common.SharedLibraries.Logging.Model;
using mmria.common.SharedLibraries.OfflineCase.DAL;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.Logging.Manager;

public sealed class LoggingManager
{
    private const int LogLimit = 100000;
    private readonly LoggingDAL _loggingDal;
    private readonly OfflineCaseDAL _offlineCaseDal;

    public LoggingManager(LoggingDAL loggingDal, OfflineCaseDAL offlineCaseDal)
    {
        _loggingDal = loggingDal;
        _offlineCaseDal = offlineCaseDal;
    }

    public async Task<dynamic> GetLoggingByOfflineSessionAsync(DBConfigurationDetail dbConfig)
    {
        string response = await _loggingDal.GetLoggingByOfflineSessionViewJsonAsync(dbConfig);
        return JsonConvert.DeserializeObject<dynamic>(response);
    }

    public async Task<dynamic> GetOfflineSessionsAsync(DBConfigurationDetail dbConfig)
    {
        string response = await _offlineCaseDal.GetLightweightStatusOnlyViewJsonAsync(dbConfig);
        return JsonConvert.DeserializeObject<dynamic>(response);
    }

    public async Task<object> GetLogsAsync(
        LoggingLogQuery query,
        bool restrictToCurrentUser,
        string currentUserName,
        DBConfigurationDetail dbConfig)
    {
        query ??= new LoggingLogQuery();

        var effectiveUserName = query.userName;
        if (restrictToCurrentUser)
        {
            effectiveUserName = currentUserName;
        }

        string response = await _loggingDal.GetLogsViewJsonAsync(query, restrictToCurrentUser, LogLimit, dbConfig);
        var data = JsonConvert.DeserializeObject<dynamic>(response);

        var logs = new List<object>();

        if (data?.rows != null)
        {
            foreach (var row in data.rows)
            {
                var doc = row.doc ?? row.value;
                if (doc == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(query.level) && query.level.ToLower() != "all")
                {
                    if (doc.level == null || doc.level.ToString().ToLower() != query.level.ToLower())
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(query.context) && query.context.ToLower() != "all")
                {
                    if (doc.context == null || doc.context.ToString() != query.context)
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(query.sessionId) && query.sessionId.ToLower() != "all")
                {
                    if (doc.offline_session_id == null || doc.offline_session_id.ToString() != query.sessionId)
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(effectiveUserName) && effectiveUserName.ToLower() != "all")
                {
                    if (doc.user_name == null ||
                        !string.Equals(doc.user_name.ToString(), effectiveUserName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(query.search))
                {
                    var searchLower = query.search.ToLower();
                    var message = doc.message?.ToString()?.ToLower() ?? "";
                    var ctx = doc.context?.ToString()?.ToLower() ?? "";

                    if (!message.Contains(searchLower) && !ctx.Contains(searchLower))
                    {
                        continue;
                    }
                }

                DateTime start = DateTime.MinValue;
                DateTime end = DateTime.MaxValue;
                DateTime startLogTime = DateTime.MinValue;
                DateTime endLogTime = DateTime.MinValue;

                if (!string.IsNullOrWhiteSpace(query.startDate) && DateTime.TryParse(query.startDate, out start))
                {
                    if (doc.timestamp != null && DateTime.TryParse(doc.timestamp.ToString(), out startLogTime))
                    {
                        if (startLogTime < start)
                        {
                            continue;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(query.endDate) && DateTime.TryParse(query.endDate, out end))
                {
                    if (doc.timestamp != null && DateTime.TryParse(doc.timestamp.ToString(), out endLogTime))
                    {
                        if (endLogTime > end)
                        {
                            continue;
                        }
                    }
                }

                logs.Add(doc);

                if (logs.Count >= LogLimit)
                {
                    break;
                }
            }
        }

        return new
        {
            logs = logs.OrderBy(l =>
                {
                    DateTime logTime = DateTime.MinValue;
                    if (l.GetType().GetProperty("timestamp") != null)
                    {
                        DateTime.TryParse(l.GetType().GetProperty("timestamp").GetValue(l)?.ToString(), out logTime);
                    }

                    return logTime;
                })
                .Skip(query.skip)
                .Take(LogLimit)
                .ToList(),
            total = logs.Count,
            limit = LogLimit,
            skip = query.skip
        };
    }

    public async Task<document_put_response> SaveLogEntryAsync(object logEntry, DBConfigurationDetail dbConfig)
    {
        try
        {
            return await _loggingDal.SaveLogEntryAsync(logEntry, dbConfig);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SaveLog error: {ex}");
            return new document_put_response
            {
                ok = false,
                error_description = "Failed to save log entry."
            };
        }
    }
}
