using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using mmria.server.extension;

namespace mmria.server.Controllers;


public sealed class loggerController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public loggerController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    [Authorize(Roles = "form_designer, installation_admin, cdc_admin")]
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = "form_designer, installation_admin, cdc_admin")]
    [HttpGet("api/logger/metadata")]    
    public async Task<IActionResult> GetMetadata()
    {
        try
        {
            string dbUrl = $"{db_config.url}/{db_config.prefix}logging";
         
            // Get distinct modules/contexts using by-context view with group=true
            var modulesUrl = $"{dbUrl}/_design/sortable/_view/by-context";
            var modulesCurl = new cURL("GET", null, modulesUrl, null, 
                db_config.user_name, db_config.user_value);
            var modulesResponse = await modulesCurl.executeAsync();
            var modulesData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(modulesResponse);
            
            var modules = new HashSet<string>();
            if (modulesData?.rows != null)
            {
                foreach (var row in modulesData.rows)
                {
                    if (row.key != null && !string.IsNullOrWhiteSpace(row.key.ToString()))
                    {
                        modules.Add(row.key.ToString());
                    }
                }
            }
            

            string offlineSessionsUrl = db_config.Get_Prefix_DB_Url("offline_cases/_design/sortable/_view/all-sessions");
            var offlineSessionsCurl = new cURL("GET", null, offlineSessionsUrl, null, 
                db_config.user_name, db_config.user_value);
            var offlineSessionsResponse = await offlineSessionsCurl.executeAsync();
            var offlineSessionsData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(offlineSessionsResponse);

            var offlineSessions = new List<object>();
            if (offlineSessionsData?.rows != null)
            {
                foreach (var row in offlineSessionsData.rows)
                {
                    if (row?.value != null)
                    {
                        var sessionId = row.value._id?.ToString();
                        var createdBy = row.value.created_by?.ToString() ?? "Unknown";
                        var dateCreated = row.value.date_created?.ToString();
                        var dateLastUpdated = row.value.date_last_updated?.ToString();
                        var offlineState = row.value.offline_state?.ToString() ?? "0";
                        
                        if (!string.IsNullOrWhiteSpace(sessionId))
                        {
                            DateTime createdDate = DateTime.MinValue;
                            DateTime.TryParse(dateCreated, out createdDate);
                            
                            var displayName = $"{sessionId.Substring(0, Math.Min(8, sessionId.Length))}... ({createdBy}) {createdDate:yyyy-MM-dd HH:mm}";
                            var offlineStateText = GetOfflineStateText(offlineState);
                            offlineSessions.Add(new 
                            { 
                                name = displayName,
                                value = sessionId,
                                createdBy = createdBy,
                                dateCreated = createdDate,
                                dateLastUpdated = dateLastUpdated,
                                offlineState = offlineStateText,
                                // 0 = created, 1 = going back online, 2 = completed, 3 = error
                            });
                        }
                    }
                }
            }
            
            // Get distinct session IDs and their oldest timestamps from by-offline-session view
            var sessionIdsUrl = $"{dbUrl}/_design/sortable/_view/by-offline-session?include_docs=true";
            var sessionIdsCurl = new cURL("GET", null, sessionIdsUrl, null, 
                db_config.user_name, db_config.user_value);
            var sessionIdsResponse = await sessionIdsCurl.executeAsync();
            var sessionIdsData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(sessionIdsResponse);

            var sessionIdsDict = new Dictionary<string, DateTime>();
            if (sessionIdsData?.rows != null)
            {
                foreach (var row in sessionIdsData.rows)
                {
                    if (row.key != null && !string.IsNullOrWhiteSpace(row.key.ToString()))
                    {
                        var key = row.key.ToString();
                        var doc = row.doc ?? row.value;
                         DateTime timestamp=DateTime.MinValue;
                        if (doc?.timestamp != null && DateTime.TryParse(doc.timestamp.ToString(), out timestamp))
                        {
                            if (!sessionIdsDict.ContainsKey(key) || timestamp < sessionIdsDict[key])
                            {
                                sessionIdsDict[key] = timestamp;
                            }
                        }
                    }
                }
            }

        
            // Update offline sessions with hasLogData property
            offlineSessions = offlineSessions.Select(session => 
            {
                var sessionObj = (dynamic)session;
                string sessionValue = sessionObj.value;
                
                return new 
                {
                    name = (string)sessionObj.name,
                    value = sessionValue,
                    createdBy = (string)sessionObj.createdBy,
                    dateCreated = (DateTime)sessionObj.dateCreated,
                    dateLastUpdated = (string)sessionObj.dateLastUpdated,
                    offlineState = (string)sessionObj.offlineState,
                    hasLogData = sessionIdsDict.ContainsKey(sessionValue)
                };
            }).OrderByDescending(s => s.dateCreated).ToList<object>();

            var sessionIds = sessionIdsDict.Select(kvp => new 
            { 
                name = $"{kvp.Key.Substring(0, 25)}... {kvp.Value:yyyy-MM-dd HH:mm}",
                value = kvp.Key
            }).OrderByDescending(s => s.name).ToList();
            
            // Get distinct user names using by-user view with group=true
            var userNamesUrl = $"{dbUrl}/_design/sortable/_view/by-user";
            var userNamesCurl = new cURL("GET", null, userNamesUrl, null, 
                db_config.user_name, db_config.user_value);
            var userNamesResponse = await userNamesCurl.executeAsync();
            var userNamesData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(userNamesResponse);
            
            var userNames = new HashSet<string>();
            if (userNamesData?.rows != null)
            {
                foreach (var row in userNamesData.rows)
                {
                    if (row.key != null && !string.IsNullOrWhiteSpace(row.key.ToString()))
                    {
                        userNames.Add(row.key.ToString());
                    }
                }
            }
            
            return Json(new
            {               
                modules = modules.OrderBy(m => m).ToList(),           
                sessionIds = offlineSessions.OrderByDescending(s => ((DateTime)s.GetType().GetProperty("dateCreated").GetValue(s))).ToList(),
                userNames = userNames.OrderBy(u => u).ToList()
            });
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"GetMetadata error: {ex}");
            return StatusCode(500, new { error = "Failed to retrieve metadata", details = ex.Message });
        }
    }

    [HttpGet("api/logger/get-logs")]
    [Authorize(Roles = "form_designer, installation_admin, cdc_admin")]
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
            else if (!string.IsNullOrWhiteSpace(userName) && userName.ToLower() != "all")
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
            
            var curl = new cURL("GET", null, viewUrl, null, 
                db_config.user_name, db_config.user_value);
            var response = await curl.executeAsync();
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
                    
                    if (!string.IsNullOrWhiteSpace(userName) && userName.ToLower() != "all")
                    {
                        if (doc.user_name == null || doc.user_name.ToString() != userName)
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
            
            return Json(new
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
            return StatusCode(500, new { error = "Failed to retrieve logs", details = ex.Message });
        }
    }
    // Add this helper method to convert offline state to text
    string GetOfflineStateText(string offlineState)
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

    [Route("api/logger/save-offline-log-data")]
    [HttpPost("save-offline-log-data")]
    [Authorize(Roles = "abstractor, data_analyst")]      
    public async Task<IActionResult> Post([FromBody] mmria.server.model.LogEntryBatch batch)
    {
        if (batch == null || batch.logs == null || batch.logs.Length == 0)
        {
            return BadRequest(new { error = "No logs provided" });
        }

        var results = new System.Collections.Generic.List<mmria.common.model.couchdb.document_put_response>();
        var userName = "";
        
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name))
                .FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }

        foreach (var logEntry in batch.logs)
        {
            try
            {
                logEntry._id = Guid.NewGuid().ToString();
                logEntry.date_created = DateTime.UtcNow;
                logEntry.user_name = userName;

                var result = await SaveLog(logEntry);
                results.Add(result);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error saving log entry: {ex}");
                results.Add(new mmria.common.model.couchdb.document_put_response
                {
                    ok = false,
                    error_description = ex.Message
                });
            }
        }

        var successCount = results.Count(r => r.ok);
        var failureCount = results.Count(r => !r.ok);

        return Json(new
        {
            success = successCount,
            failed = failureCount,
            total = batch.logs.Length,
            results = results
        });
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

            var curl = new cURL("POST", null, url, json, 
                db_config.user_name, db_config.user_value);
            
            string response = await curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert
                .DeserializeObject<mmria.common.model.couchdb.document_put_response>(response);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"SaveLog error: {ex}");
            result.ok = false;
            result.error_description = ex.Message;
        }

        return result;
    }
}
