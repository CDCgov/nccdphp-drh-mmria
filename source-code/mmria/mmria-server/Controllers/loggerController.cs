using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using mmria.server.extension;
using mmria.server.util;

namespace mmria.server.Controllers;


public sealed class loggerController : Controller
{
    private const string LoggerViewerRoles = "form_designer, installation_admin, cdc_admin, offline_mode";
    private static readonly string[] FullLogViewerRoles = { "form_designer", "installation_admin", "cdc_admin" };

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;

    public loggerController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        _couchDbHttpClient = couchDbHttpClient;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [Authorize(Roles = LoggerViewerRoles)]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = LoggerViewerRoles)]
    [HttpGet("api/logger/metadata")]    
    public async Task<IActionResult> GetMetadata()
    {
        try
        {
            var restrictToCurrentUser = IsOfflineModeRestricted();
            var currentUserName = GetCurrentUserName();
            if (restrictToCurrentUser && string.IsNullOrWhiteSpace(currentUserName))
            {
                return Forbid();
            }

            var modulesData = await LoadLoggingByOfflineSessionAsync();
            var offlineSessionsData = await LoadOfflineSessionsAsync();

            var modules = ExtractModules(modulesData, restrictToCurrentUser, currentUserName);
            var offlineSessions = ExtractOfflineSessions(offlineSessionsData, restrictToCurrentUser, currentUserName);
            var sessionIdsWithLogs = ExtractOfflineSessionIds(modulesData, restrictToCurrentUser, currentUserName);
            var userNames = ExtractUserNames(modulesData, restrictToCurrentUser, currentUserName);

            offlineSessions = AnnotateOfflineSessionsWithLogPresence(offlineSessions, sessionIdsWithLogs);

            if (restrictToCurrentUser && !string.IsNullOrWhiteSpace(currentUserName))
            {
                userNames.Clear();
                userNames.Add(currentUserName);
            }

            return EscapedJsonResultFactory.Create(new
            {
                modules = modules.OrderBy(m => m).ToList(),
                sessionIds = offlineSessions
                    .OrderByDescending(s => ((DateTime)s.GetType().GetProperty("dateCreated").GetValue(s)))
                    .ToList(),
                userNames = userNames.OrderBy(u => u).ToList()
            });
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"GetMetadata error: {ex}");
            return StatusCode(500, new { error = "Failed to retrieve metadata", details = "An unexpected error occurred while retrieving metadata." });
        }
    }

    private async Task<dynamic> LoadLoggingByOfflineSessionAsync()
    {
        string dbUrl = $"{db_config.url}/{db_config.prefix}logging";
        var modulesUrl = $"{dbUrl}/_design/sortable/_view/by-offline-session";
        var response = await _couchDbHttpClient.ExecuteAsync("GET", modulesUrl, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
    }

    private async Task<dynamic> LoadOfflineSessionsAsync()
    {
        string url = db_config.Get_Prefix_DB_Url("offline_cases/_design/sortable/_view/lightweight-status-only");
        var response = await _couchDbHttpClient.ExecuteAsync("GET", url, null, db_config.user_name, db_config.user_value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
    }

    private static HashSet<string> ExtractModules(dynamic modulesData, bool restrictToCurrentUser, string currentUserName)
    {
        var modules = new HashSet<string>();
        if (modulesData?.rows == null)
        {
            return modules;
        }

        foreach (var row in modulesData.rows)
        {
            if (restrictToCurrentUser && !IsDynamicValueEqual(row.value.user_name, currentUserName))
            {
                continue;
            }

            if (row.value.context != null && !string.IsNullOrWhiteSpace(row.value.context.ToString()))
            {
                modules.Add(row.value.context.ToString());
            }
        }

        return modules;
    }

    private static List<object> ExtractOfflineSessions(dynamic offlineSessionsData, bool restrictToCurrentUser, string currentUserName)
    {
        var offlineSessions = new List<object>();
        if (offlineSessionsData?.rows == null)
        {
            return offlineSessions;
        }

        foreach (var row in offlineSessionsData.rows)
        {
            var sessionItem = TryBuildOfflineSession(row, restrictToCurrentUser, currentUserName);
            if (sessionItem != null)
            {
                offlineSessions.Add(sessionItem);
            }
        }

        return offlineSessions;
    }

    private static object TryBuildOfflineSession(dynamic row, bool restrictToCurrentUser, string currentUserName)
    {
        if (row?.value == null)
        {
            return null;
        }

        if (restrictToCurrentUser && !IsDynamicValueEqual(row.value.created_by, currentUserName))
        {
            return null;
        }

        var sessionId = row.value._id?.ToString();
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var dateCreated = row.value.date_created?.ToString();
        var dateLastUpdated = row.value.date_last_updated?.ToString();
        var offlineState = row.value.offline_state?.ToString() ?? "0";

        DateTime createdDate = DateTime.MinValue;
        DateTime.TryParse(dateCreated, out createdDate);

        var displayName = $"{sessionId.Substring(0, Math.Min(8, sessionId.Length))}... {createdDate:yyyy-MM-dd HH:mm}";
        var offlineStateText = GetOfflineStateText(offlineState);

        return new
        {
            name = displayName,
            value = sessionId,
            dateCreated = createdDate,
            dateLastUpdated = dateLastUpdated,
            offlineState = offlineStateText,
            // 0 = created, 1 = going back online, 2 = completed, 3 = error
        };
    }

    private static HashSet<string> ExtractOfflineSessionIds(dynamic modulesData, bool restrictToCurrentUser, string currentUserName)
    {
        var sessionIds = new HashSet<string>();
        if (modulesData?.rows == null)
        {
            return sessionIds;
        }

        foreach (var row in modulesData.rows)
        {
            if (restrictToCurrentUser && !IsDynamicValueEqual(row.value.user_name, currentUserName))
            {
                continue;
            }

            if (row.value.offline_session_id != null && !string.IsNullOrWhiteSpace(row.value.offline_session_id.ToString()))
            {
                sessionIds.Add(row.value.offline_session_id.ToString());
            }
        }

        return sessionIds;
    }

    private static HashSet<string> ExtractUserNames(dynamic modulesData, bool restrictToCurrentUser, string currentUserName)
    {
        var userNames = new HashSet<string>();
        if (modulesData?.rows == null)
        {
            return userNames;
        }

        foreach (var row in modulesData.rows)
        {
            if (restrictToCurrentUser && !IsDynamicValueEqual(row.value.user_name, currentUserName))
            {
                continue;
            }

            if (row.value.user_name != null && !string.IsNullOrWhiteSpace(row.value.user_name.ToString()))
            {
                userNames.Add(row.value.user_name.ToString());
            }
        }

        return userNames;
    }

    private static List<object> AnnotateOfflineSessionsWithLogPresence(List<object> offlineSessions, HashSet<string> sessionIdsWithLogs)
    {
        return offlineSessions.Select(session =>
        {
            var sessionObj = (dynamic)session;
            string sessionValue = sessionObj.value;

            return new
            {
                name = (string)sessionObj.name,
                value = sessionValue,
                dateCreated = (DateTime)sessionObj.dateCreated,
                dateLastUpdated = (string)sessionObj.dateLastUpdated,
                offlineState = (string)sessionObj.offlineState,
                hasLogData = sessionIdsWithLogs.Contains(sessionValue)
            };
        }).OrderByDescending(s => s.dateCreated).ToList<object>();
    }

    [HttpGet("api/logger/get-logs")]
    [Authorize(Roles = LoggerViewerRoles)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string level = null,
        [FromQuery] string context = null,
        [FromQuery] string sessionId = null,
        [FromQuery] string userName = null,
        [FromQuery] string search = null,
        [FromQuery] string startDate = null,
        [FromQuery] string endDate = null,
        //[FromQuery] int limit = 100000,
        [FromQuery] int skip = 0)
    {
        int limit = 100000;
        try
        {
            var restrictToCurrentUser = IsOfflineModeRestricted();
            var effectiveUserName = userName;
            if (restrictToCurrentUser)
            {
                effectiveUserName = GetCurrentUserName();
                if (string.IsNullOrWhiteSpace(effectiveUserName))
                {
                    return Forbid();
                }
            }

            string dbUrl = $"{db_config.url}/{db_config.prefix}logging";
            string viewUrl;
            
            // Select the most appropriate view based on filters (priority order for performance)
            // If date range is provided, prefer the by-timestamp view (most appropriate for date queries)
            if ((!string.IsNullOrWhiteSpace(startDate) && startDate.ToLower() != "all") || (!string.IsNullOrWhiteSpace(endDate) && endDate.ToLower() != "all"))
            {
                // Try to parse start/end dates to ISO 8601; if parsing fails, use the raw input
                DateTime startDt=DateTime.MinValue;
                DateTime endDt=DateTime.MaxValue;
                bool hasStart = !string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out startDt);
                bool hasEnd = !string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out endDt);

                string startKeyIso;
                string endKeyIso;

                if (hasStart && hasEnd)
                {
                    // For descending=true, startkey must be the later timestamp and endkey the earlier
                    var later = startDt > endDt ? startDt : endDt;
                    var earlier = startDt > endDt ? endDt : startDt;
                    startKeyIso = later.ToString("o");
                    endKeyIso = earlier.ToString("o");
                }
                else if (hasStart)
                {
                    // From start to max
                    startKeyIso = DateTime.MaxValue.ToString("o");
                    endKeyIso = startDt.ToString("o");
                }
                else if (hasEnd)
                {
                    // From min to end
                    startKeyIso = endDt.ToString("o");
                    endKeyIso = DateTime.MinValue.ToString("o");
                }
                else
                {
                    // Use raw strings if parsing not possible
                    startKeyIso = !string.IsNullOrWhiteSpace(endDate) ? endDate : DateTime.MaxValue.ToString("o");
                    endKeyIso = !string.IsNullOrWhiteSpace(startDate) ? startDate : DateTime.MinValue.ToString("o");
                }

                // JSON string keys and URL-encode
                var encodedStart = System.Web.HttpUtility.UrlEncode($"\"{startKeyIso}\"");
                var encodedEnd = System.Web.HttpUtility.UrlEncode($"\"{endKeyIso}\"");

                // Query by-timestamp with startkey/endkey (descending=true) and limit
                viewUrl = $"{dbUrl}/_design/sortable/_view/by-timestamp?include_docs=true&startkey={encodedStart}&endkey={encodedEnd}&descending=true&limit={limit}";
            }
            // Query the most selective filter first to minimize data transfer
            else if (!string.IsNullOrWhiteSpace(sessionId) && sessionId.ToLower() != "all")
            {
                // Use by-offline-session view - most selective, returns exact session
                var encodedKey = System.Web.HttpUtility.UrlEncode($"\"{sessionId}\"");
                viewUrl = $"{dbUrl}/_design/sortable/_view/by-offline-session?key={encodedKey}&include_docs=true&descending=true";
            }
            else if (!restrictToCurrentUser && !string.IsNullOrWhiteSpace(userName) && userName.ToLower() != "all")
            {
                // Use by-user view - selective by user
                var encodedKey = System.Web.HttpUtility.UrlEncode($"\"{userName}\"");
                viewUrl = $"{dbUrl}/_design/sortable/_view/by-user?key={encodedKey}&include_docs=true&descending=true";
            }
            else if (!string.IsNullOrWhiteSpace(context) && context.ToLower() != "all")
            {
                // Use by-context view - selective by module
                var encodedKey = System.Web.HttpUtility.UrlEncode($"\"{context}\"");
                viewUrl = $"{dbUrl}/_design/sortable/_view/by-context?key={encodedKey}&include_docs=true&descending=true";
            }
            else if (!string.IsNullOrWhiteSpace(level) && level.ToLower() != "all")
            {
                // Use by-level view - selective by log level
                var encodedKey = System.Web.HttpUtility.UrlEncode($"\"{level.ToLower()}\"");
                viewUrl = $"{dbUrl}/_design/sortable/_view/by-level?key={encodedKey}&include_docs=true&descending=true";
            }
            else
            {
                // No specific filter - use all-fields view with limit
                viewUrl = $"{dbUrl}/_design/sortable/_view/all-fields?include_docs=true&limit={limit}&skip={skip}&descending=true";
            }
            
            var response = await _couchDbHttpClient.ExecuteAsync("GET", viewUrl, null, db_config.user_name, db_config.user_value);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
            
            var logs = new List<object>();
            
            if (data?.rows != null)
            {
                foreach (var row in data.rows)
                {
                    var doc = row.doc ?? row.value;
                    if (doc == null) continue;
                    
                    // Apply remaining filters that weren't used in the view query
                    if (!string.IsNullOrWhiteSpace(level) && level.ToLower() != "all")
                    {
                        if (doc.level == null || doc.level.ToString().ToLower() != level.ToLower())
                            continue;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(context) && context.ToLower() != "all")
                    {
                        if (doc.context == null || doc.context.ToString() != context)
                            continue;
                    }

                    if (!string.IsNullOrWhiteSpace(sessionId) && sessionId.ToLower() != "all")
                    {
                        if (doc.offline_session_id == null || doc.offline_session_id.ToString() != sessionId)
                            continue;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(effectiveUserName) && effectiveUserName.ToLower() != "all")
                    {
                        if (doc.user_name == null ||
                            !string.Equals(doc.user_name.ToString(), effectiveUserName, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var searchLower = search.ToLower();
                        var message = doc.message?.ToString()?.ToLower() ?? "";
                        var ctx = doc.context?.ToString()?.ToLower() ?? "";
                        
                        if (!message.Contains(searchLower) && !ctx.Contains(searchLower))
                            continue;
                    }
                                            
                    // Date filtering
                    DateTime start = DateTime.MinValue;
                    DateTime end = DateTime.MaxValue;
                    DateTime startLogTime = DateTime.MinValue;
                    DateTime endLogTime = DateTime.MinValue;
                    
                    
                    if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out start))
                    {
                        if (doc.timestamp != null && DateTime.TryParse(doc.timestamp.ToString(), out startLogTime))
                        {
                            if (startLogTime < start)
                            {
                                continue;
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out end))
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
                    
                    // Apply limit when using filtered views (they don't have limit in URL)
                    if (logs.Count >= limit)
                    {
                        break;
                    }
                }
            }
            
            return EscapedJsonResultFactory.Create(new
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
                    .Skip(skip)
                    .Take(limit)
                    .ToList(),
                total = logs.Count,
                limit = limit,
                skip = skip
            });
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"GetLogs error: {ex}");
            return StatusCode(500, new { error = "Failed to retrieve logs", details = "An unexpected error occurred while retrieving logs." });
        }
    }
    // Add this helper method to convert offline state to text
    private static string GetOfflineStateText(string offlineState)
    {
        return offlineState switch
        {
            "0" => "created",
            "1" => "going back online",
            "2" => "completed",
            "3" => "error",
            _ => "unknown"
        };
    }

    private bool IsOfflineModeRestricted()
    {
        return User.IsInRole("offline_mode") &&
            !FullLogViewerRoles.Any(role => User.IsInRole(role));
    }

    private string GetCurrentUserName()
    {
        return User.Identities.FirstOrDefault(
                identity => identity.IsAuthenticated &&
                    identity.HasClaim(claim => claim.Type == ClaimTypes.Name))
            ?.FindFirst(ClaimTypes.Name)
            ?.Value;
    }

    private static bool IsDynamicValueEqual(dynamic value, string expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        var actual = DynamicValueToString(value);
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string DynamicValueToString(dynamic value)
    {
        if (value == null)
        {
            return null;
        }

        try
        {
            var text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }

[Route("api/logger/save-offline-log-data")]
[HttpPost("save-offline-log-data")]
[Authorize(Roles = "abstractor, data_analyst")]      
public async Task<IActionResult> Post()
{
    var batch = await JsonRequestBodyReader.ReadAsync<mmria.server.model.LogEntryBatch>(Request);
    var sanitizedBatch = CreateSanitizedLogBatch(batch);
    if (sanitizedBatch == null || sanitizedBatch.logs == null || sanitizedBatch.logs.Length == 0)
    {
        return BadRequest(new { error = "No logs provided" });
    }

    // Get username NOW before context is disposed
    var userName = "";
    if (User.Identities.Any(u => u.IsAuthenticated))
    {
        userName = User.Identities.First(
            u => u.IsAuthenticated && 
            u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
            .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
    }

    // Fire-and-forget: Process logs in background without blocking response
    _ = Task.Run(async () => 
    {
        try
        {
            foreach (var logEntry in sanitizedBatch.logs)
            {
                try
                {
                    logEntry._id = Guid.NewGuid().ToString();
                    logEntry.date_created = DateTime.UtcNow;
                    logEntry.user_name = userName;

                    await SaveLog(logEntry);
                }
                catch (Exception ex)
                {
                    // Log to console but continue processing other logs
                    System.Console.WriteLine($"Error saving log entry (silent fail): {ex}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error in background log save operation: {ex}");
        }
    });

    // Return immediately
    return Json(new
    {
        accepted = true,
        message = "Logs accepted for processing",
        total = sanitizedBatch.logs.Length
    });
}

    private static mmria.server.model.LogEntryBatch CreateSanitizedLogBatch(mmria.server.model.LogEntryBatch batch)
    {
        if (batch?.logs == null || batch.logs.Length == 0)
        {
            return null;
        }

        var logs = batch.logs
            .Where(log => log != null)
            .Select(CreateSanitizedLogEntry)
            .Where(log => log != null)
            .ToArray();

        if (logs.Length == 0)
        {
            return null;
        }

        return new mmria.server.model.LogEntryBatch
        {
            logs = logs
        };
    }

    private static mmria.server.model.LogEntry CreateSanitizedLogEntry(mmria.server.model.LogEntry request)
    {
        if (request == null)
        {
            return null;
        }

        return new mmria.server.model.LogEntry
        {
            data_type = "log_entry",
            timestamp = request.timestamp,
            level = NormalizeOptionalString(request.level),
            context = NormalizeOptionalString(request.context),
            message = request.message,
            fileName = request.fileName,
            lineNumber = request.lineNumber,
            columnNumber = request.columnNumber,
            functionName = request.functionName,
            stackTrace = request.stackTrace,
            errorType = NormalizeOptionalString(request.errorType),
            is_offline = NormalizeOptionalString(request.is_offline),
            process_offline_cases = NormalizeOptionalString(request.process_offline_cases),
            offline_session_id = NormalizeOptionalString(request.offline_session_id)
        };
    }

    private static string NormalizeOptionalString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<mmria.common.model.couchdb.document_put_response> SaveLog(mmria.server.model.LogEntry logEntry)
    {
        var result = new mmria.common.model.couchdb.document_put_response();

        try
        {
            string url = $"{db_config.url}/{db_config.prefix}logging";
            
            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(logEntry, settings);

            string response = await _couchDbHttpClient.ExecuteAsync("POST", url, json, db_config.user_name, db_config.user_value);
            result = Newtonsoft.Json.JsonConvert
                .DeserializeObject<mmria.common.model.couchdb.document_put_response>(response);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"SaveLog error: {ex}");
            result.ok = false;
            result.error_description = "Failed to save log entry.";
        }

        return result;
    }
}
