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
using mmria.common.SharedLibraries.Logging.Manager;
using mmria.common.SharedLibraries.Logging.Model;

namespace mmria.server.Controllers;


public sealed class loggerController : Controller
{
    private const string LoggerViewerRoles = "form_designer, installation_admin, cdc_admin, offline_mode";
    private static readonly string[] FullLogViewerRoles = { "form_designer", "installation_admin", "cdc_admin" };

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly LoggingManager _loggingManager;

    public loggerController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        LoggingManager loggingManager
    )
    {
        _loggingManager = loggingManager;
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

            var modulesData = await _loggingManager.GetLoggingByOfflineSessionAsync(db_config);
            var offlineSessionsData = await _loggingManager.GetOfflineSessionsAsync(db_config);

            HashSet<string> modules = ExtractModules(modulesData, restrictToCurrentUser, currentUserName);
            List<object> offlineSessions = ExtractOfflineSessions(offlineSessionsData, restrictToCurrentUser, currentUserName);
            HashSet<string> sessionIdsWithLogs = ExtractOfflineSessionIds(modulesData, restrictToCurrentUser, currentUserName);
            HashSet<string> userNames = ExtractUserNames(modulesData, restrictToCurrentUser, currentUserName);

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
        try
        {
            var restrictToCurrentUser = IsOfflineModeRestricted();
            var currentUserName = userName;
            if (restrictToCurrentUser)
            {
                currentUserName = GetCurrentUserName();
                if (string.IsNullOrWhiteSpace(currentUserName))
                {
                    return Forbid();
                }
            }

            var result = await _loggingManager.GetLogsAsync(
                new LoggingLogQuery
                {
                    level = level,
                    context = context,
                    sessionId = sessionId,
                    userName = userName,
                    search = search,
                    startDate = startDate,
                    endDate = endDate,
                    skip = skip
                },
                restrictToCurrentUser,
                currentUserName,
                db_config);

            return EscapedJsonResultFactory.Create(result);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"GetLogs error: {ex}");
            return StatusCode(500, new { error = "Failed to retrieve logs", details = "An unexpected error occurred while retrieving logs." });
        }
    }

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
        return await _loggingManager.SaveLogEntryAsync(logEntry, db_config);
    }
}
