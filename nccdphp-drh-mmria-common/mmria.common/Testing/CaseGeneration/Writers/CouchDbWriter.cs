using mmria.common.Testing.CaseGeneration.Models;
using System.Text;
using System.Text.Json;
using System.Linq;
using mmria.common.getset;

namespace mmria.common.Testing.CaseGeneration.Writers
{
    /// <summary>
    /// Writes generated case data directly to CouchDB
    /// </summary>
    public class CouchDbWriter
    {
        private const int BulkSaveBatchSize = 100;

        private readonly GenerationConfig _config;
        private readonly CouchDbHttpClient _couchDbHttpClient;
        private readonly string _databaseUrl;
        private readonly string? _username;
        private readonly string? _password;
        private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public CouchDbWriter(GenerationConfig config, CouchDbHttpClient couchDbHttpClient)
        {
            _config = config;
            _couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));

            if (string.IsNullOrEmpty(config.CouchDbUrl))
            {
                throw new ArgumentException("CouchDB URL is required for direct save");
            }

            // Build database URL
            var dbName = config.DatabaseName ?? "mmria";
            _databaseUrl = $"{config.CouchDbUrl.TrimEnd('/')}/{dbName}";
            _username = config.CouchDbUsername;
            _password = config.CouchDbPassword;
        }

        /// <summary>
        /// Save a single case to CouchDB
        /// </summary>
        public async Task<(bool success, string? documentId, string? error)> SaveCaseAsync(Dictionary<string, object?> caseData, int caseNumber)
        {
            try
            {
                // Ensure _id exists
                if (!caseData.ContainsKey("_id") || caseData["_id"] == null)
                {
                    caseData["_id"] = $"{_config.Jurisdiction}-{caseNumber:D6}-{Guid.NewGuid():N}";
                }

                var documentId = caseData["_id"]!.ToString()!;

                // Serialize case data to bytes to avoid heap inspection of sensitive string data
                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(caseData, s_jsonSerializerOptions);

                // PUT to CouchDB using CouchDbHttpClient with byte payload
                var responseBody = await _couchDbHttpClient.ExecuteBytesAsync(
                    method: "PUT",
                    url: $"{_databaseUrl}/{documentId}",
                    payloadBytes: jsonBytes,
                    userName: _username,
                    password: _password,
                    contentType: "application/json"
                );

                var result = JsonSerializer.Deserialize<CouchDbResponse>(responseBody);
                
                return (true, result?.id ?? documentId, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Save multiple cases to CouchDB with progress reporting
        /// </summary>
        public async Task<CouchDbBatchResult> SaveCasesBatchAsync(
            List<Dictionary<string, object?>> cases,
            IProgress<int>? progress = null)
        {
            var result = new CouchDbBatchResult
            {
                TotalCases = cases.Count
            };

            for (int batchStart = 0; batchStart < cases.Count; batchStart += BulkSaveBatchSize)
            {
                var batchItems = cases
                    .Skip(batchStart)
                    .Take(BulkSaveBatchSize)
                    .Select((caseData, index) => PrepareCaseDocument(caseData, batchStart + index + 1))
                    .ToList();

                try
                {
                    var responseItems = await SaveCaseBatchAsync(batchItems);
                    var responseById = responseItems
                        .Where(item => !string.IsNullOrWhiteSpace(item.id))
                        .GroupBy(item => item.id!, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

                    for (int index = 0; index < batchItems.Count; index++)
                    {
                        var preparedCase = batchItems[index];
                        responseById.TryGetValue(preparedCase.DocumentId, out var responseItem);

                        if
                        (
                            responseItem == null &&
                            index < responseItems.Count &&
                            string.Equals(responseItems[index].id, preparedCase.DocumentId, StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            responseItem = responseItems[index];
                        }

                        if (responseItem?.ok == true)
                        {
                            result.SuccessCount++;
                            result.SavedDocumentIds.Add(responseItem.id ?? preparedCase.DocumentId);
                        }
                        else
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Case {preparedCase.CaseNumber}: {BuildBulkErrorMessage(responseItem)}");
                        }

                        progress?.Report(preparedCase.CaseNumber);
                    }
                }
                catch (Exception ex)
                {
                    foreach (var preparedCase in batchItems)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Case {preparedCase.CaseNumber}: Exception: {ex.Message}");
                        progress?.Report(preparedCase.CaseNumber);
                    }
                }
            }

            return result;
        }

        private PreparedCaseDocument PrepareCaseDocument(Dictionary<string, object?> caseData, int caseNumber)
        {
            if (!caseData.ContainsKey("_id") || caseData["_id"] == null)
            {
                caseData["_id"] = $"{_config.Jurisdiction}-{caseNumber:D6}-{Guid.NewGuid():N}";
            }

            return new PreparedCaseDocument
            {
                CaseData = caseData,
                CaseNumber = caseNumber,
                DocumentId = caseData["_id"]!.ToString()!
            };
        }

        private async Task<List<CouchDbBulkResponseItem>> SaveCaseBatchAsync(List<PreparedCaseDocument> preparedCases)
        {
            if (preparedCases.Count == 0)
            {
                return new List<CouchDbBulkResponseItem>();
            }

            var requestPayload = new CouchDbBulkRequest
            {
                docs = preparedCases.Select(item => item.CaseData).ToList()
            };

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(requestPayload, s_jsonSerializerOptions);
            var responseBody = await _couchDbHttpClient.ExecuteBytesAsync(
                method: "POST",
                url: $"{_databaseUrl}/_bulk_docs",
                payloadBytes: jsonBytes,
                userName: _username,
                password: _password,
                contentType: "application/json",
                throwOnError: true
            );

            return JsonSerializer.Deserialize<List<CouchDbBulkResponseItem>>(responseBody, s_jsonSerializerOptions) ?? new List<CouchDbBulkResponseItem>();
        }

        private static string BuildBulkErrorMessage(CouchDbBulkResponseItem? responseItem)
        {
            if (responseItem == null)
            {
                return "No response returned from CouchDB bulk save.";
            }

            if (!string.IsNullOrWhiteSpace(responseItem.reason))
            {
                return $"{responseItem.error}: {responseItem.reason}";
            }

            if (!string.IsNullOrWhiteSpace(responseItem.error))
            {
                return responseItem.error!;
            }

            return "Unknown CouchDB bulk save failure.";
        }

        /// <summary>
        /// Test connection to CouchDB
        /// </summary>
        public async Task<(bool success, string? error)> TestConnectionAsync()
        {
            try
            {
                var responseBody = await _couchDbHttpClient.ExecuteAsync(
                    method: "GET",
                    url: _databaseUrl,
                    userName: _username,
                    password: _password
                );

                // If we got a response without exception, connection is successful
                return (true, null);
            }
            catch (HttpRequestException ex)
            {
                // Check if it's a 404 (database not found)
                if (ex.Message.Contains("404") || ex.Message.Contains("not found"))
                {
                    return (false, $"Database not found: {_databaseUrl}. Create it first or check the name.");
                }
                // Check if it's a 401 (unauthorized)
                else if (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
                {
                    return (false, "Authentication failed. Check username and password.");
                }
                else
                {
                    return (false, $"Connection error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Create database if it doesn't exist
        /// </summary>
        public async Task<(bool success, string? error)> CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                // Check if database exists
                try
                {
                    await _couchDbHttpClient.ExecuteAsync(
                        method: "GET",
                        url: _databaseUrl,
                        userName: _username,
                        password: _password
                    );
                    return (true, "Database already exists");
                }
                catch (HttpRequestException ex)
                {
                    if (!ex.Message.Contains("404") && !ex.Message.Contains("not found"))
                    {
                        throw;
                    }
                }

                // Create database
                var responseBody = await _couchDbHttpClient.ExecuteAsync(
                    method: "PUT",
                    url: _databaseUrl,
                    userName: _username,
                    password: _password,
                    throwOnError: true
                );
                
                return (true, "Database created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating database: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// CouchDB response structure
    /// </summary>
    public class CouchDbResponse
    {
        public bool ok { get; set; }
        public string? id { get; set; }
        public string? rev { get; set; }
    }

    internal sealed class CouchDbBulkRequest
    {
        public List<Dictionary<string, object?>> docs { get; set; } = new();
    }

    internal sealed class CouchDbBulkResponseItem
    {
        public bool ok { get; set; }
        public string? id { get; set; }
        public string? rev { get; set; }
        public string? error { get; set; }
        public string? reason { get; set; }
    }

    internal sealed class PreparedCaseDocument
    {
        public int CaseNumber { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public Dictionary<string, object?> CaseData { get; set; } = new();
    }

    /// <summary>
    /// Result of batch save operation
    /// </summary>
    public class CouchDbBatchResult
    {
        public int TotalCases { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> SavedDocumentIds { get; set; } = new();
        public List<string> Errors { get; set; } = new();

        public double SuccessRate => TotalCases > 0 ? (double)SuccessCount / TotalCases * 100 : 0;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== CouchDB Save Results ===");
            sb.AppendLine($"Total Cases: {TotalCases}");
            sb.AppendLine($"Successfully Saved: {SuccessCount} ({SuccessRate:F1}%)");
            sb.AppendLine($"Failed: {FailureCount}");

            if (Errors.Count > 0)
            {
                sb.AppendLine($"\nErrors ({Errors.Count}):");
                foreach (var error in Errors.Take(10))
                {
                    sb.AppendLine($"  - {error}");
                }
                if (Errors.Count > 10)
                {
                    sb.AppendLine($"  ... and {Errors.Count - 10} more errors");
                }
            }

            return sb.ToString();
        }
    }
}

