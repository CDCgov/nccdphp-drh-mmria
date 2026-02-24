#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mmria.common.getset;

namespace mmria_server.tests;

/// <summary>
/// CaseDataHelper provides utilities for creating, manipulating, and validating case data.
/// Simplifies test data setup and common case operations.
/// </summary>
public class CaseDataHelper
{
    private readonly CouchDbHttpClient _httpClient;
    private readonly string _databaseUrl;
    private readonly string? _userName;
    private readonly string? _password;

    public CaseDataHelper(CouchDbHttpClient httpClient, string databaseUrl, string? userName = null, string? password = null)
    {
        _httpClient = httpClient;
        _databaseUrl = databaseUrl;
        _userName = userName;
        _password = password;
    }

    /// <summary>
    /// Creates a minimal valid case object
    /// </summary>
    public Dictionary<string, object> CreateMinimalCase(string caseId)
    {
        return new Dictionary<string, object>
        {
            { "_id", caseId },
            { "case_number", GenerateCaseNumber() },
            { "jurisdiction", "jurisdiction1" },
            { "status", "open" },
            { "created_date", DateTime.UtcNow },
            { "created_by", "test_user" }
        };
    }

    /// <summary>
    /// Creates a complete case object with all standard fields
    /// </summary>
    public Dictionary<string, object> CreateCompleteCase(string caseId, Dictionary<string, object>? overrides = null)
    {
        var case_data = new Dictionary<string, object>
        {
            { "_id", caseId },
            { "case_number", GenerateCaseNumber() },
            { "jurisdiction", "jurisdiction1" },
            { "status", "open" },
            { "created_date", DateTime.UtcNow },
            { "created_by", "test_user" },
            { "last_modified_date", DateTime.UtcNow },
            { "last_modified_by", "test_user" },
            { "abstractor", "test_abstractor" },
            { "reviewer", null },
            { "reviewed_date", null },
            { "data_analyst", null },
            { "analysis_lock", false },
            { "summary", "Test case" },
            { "case_type", "maternal_death" }
        };

        if (overrides != null)
        {
            foreach (var kvp in overrides)
            {
                case_data[kvp.Key] = kvp.Value;
            }
        }

        return case_data;
    }

    /// <summary>
    /// Generate a unique case number for testing
    /// </summary>
    public string GenerateCaseNumber()
    {
        return $"TEST-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
    }

    /// <summary>
    /// Persist case to CouchDB
    /// </summary>
    public async Task<string> SaveCaseAsync(Dictionary<string, object> caseData)
    {
        var caseId = caseData["_id"].ToString()!;
        var url = $"{_databaseUrl}/{caseId}";

        // TODO: Serialize case_data to JSON and POST to URL
        // await _httpClient.ExecuteAsync("POST", url, ...)
        
        await Task.CompletedTask;
        return caseId;
    }

    /// <summary>
    /// Retrieve case from CouchDB
    /// </summary>
    public async Task<Dictionary<string, object>?> GetCaseAsync(string caseId)
    {
        var url = $"{_databaseUrl}/{caseId}";
        
        // TODO: GET from URL and deserialize to Dictionary
        // var response = await _httpClient.ExecuteAsync("GET", url, ...)
        
        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// Update case status
    /// </summary>
    public async Task UpdateCaseStatusAsync(string caseId, string newStatus)
    {
        var case_data = await GetCaseAsync(caseId);
        if (case_data != null)
        {
            case_data["status"] = newStatus;
            case_data["last_modified_date"] = DateTime.UtcNow;
            case_data["last_modified_by"] = "test_user";
            await SaveCaseAsync(case_data);
        }
    }

    /// <summary>
    /// Assign case to abstractor
    /// </summary>
    public async Task AssignCaseToAbstractorAsync(string caseId, string abstractorId)
    {
        var case_data = await GetCaseAsync(caseId);
        if (case_data != null)
        {
            case_data["abstractor"] = abstractorId;
            case_data["status"] = "assigned";
            case_data["last_modified_date"] = DateTime.UtcNow;
            await SaveCaseAsync(case_data);
        }
    }

    /// <summary>
    /// Validate case has all required fields
    /// </summary>
    public bool ValidateCaseStructure(Dictionary<string, object> case_data)
    {
        var requiredFields = new[] { "_id", "case_number", "jurisdiction", "status", "created_date" };
        foreach (var field in requiredFields)
        {
            if (!case_data.ContainsKey(field) || case_data[field] == null)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Validate case against constraints
    /// </summary>
    public List<string> ValidateCaseData(Dictionary<string, object> case_data)
    {
        var errors = new List<string>();

        // Check required fields
        if (!case_data.ContainsKey("case_number") || string.IsNullOrEmpty(case_data["case_number"].ToString()))
            errors.Add("case_number is required");

        if (!case_data.ContainsKey("jurisdiction") || string.IsNullOrEmpty(case_data["jurisdiction"].ToString()))
            errors.Add("jurisdiction is required");

        // Check valid status values
        var validStatuses = new[] { "open", "assigned", "completed", "archived" };
        if (case_data.ContainsKey("status"))
        {
            var status = case_data["status"].ToString();
            if (!string.IsNullOrEmpty(status) && !validStatuses.Any(s => s == status))
                errors.Add($"invalid status: {status}");
        }

        return errors;
    }

    /// <summary>
    /// Create a search query for cases
    /// </summary>
    public QueryBuilder CreateQuery()
    {
        return new QueryBuilder();
    }
}

/// <summary>
/// QueryBuilder provides a fluent interface for building CouchDB queries
/// </summary>
public class QueryBuilder
{
    private readonly Dictionary<string, object> _selector = new();

    public QueryBuilder WithStatus(string status)
    {
        _selector["status"] = status;
        return this;
    }

    public QueryBuilder WithJurisdiction(string jurisdiction)
    {
        _selector["jurisdiction"] = jurisdiction;
        return this;
    }

    public QueryBuilder WithAbstractor(string abstractor)
    {
        _selector["abstractor"] = abstractor;
        return this;
    }

    public QueryBuilder WithCaseNumber(string caseNumber)
    {
        _selector["case_number"] = caseNumber;
        return this;
    }

    public QueryBuilder WithCreatedAfter(DateTime date)
    {
        if (!_selector.ContainsKey("created_date"))
            _selector["created_date"] = new Dictionary<string, object>();

        ((Dictionary<string, object>)_selector["created_date"])["$gte"] = date;
        return this;
    }

    public QueryBuilder WithCreatedBefore(DateTime date)
    {
        if (!_selector.ContainsKey("created_date"))
            _selector["created_date"] = new Dictionary<string, object>();

        ((Dictionary<string, object>)_selector["created_date"])["$lte"] = date;
        return this;
    }

    public Dictionary<string, object> Build()
    {
        return new Dictionary<string, object> { { "selector", _selector } };
    }
}
