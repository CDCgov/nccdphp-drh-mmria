using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.ManageUsers.Model;

namespace mmria.common.SharedLibraries.Jurisdiction.DAL;

/// <summary>
/// Data Access Layer for all jurisdiction CouchDB operations.
/// Implements IJurisdictionRepository — the SQL migration seam for the jurisdiction database.
/// No business logic — only data operations.
/// </summary>
public sealed class JurisdictionDAL : IJurisdictionRepository
{
    private static readonly JsonSerializerSettings IgnoreNullSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly CouchDbHttpClient _httpClient;

    public JurisdictionDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ── User-Role-Jurisdiction document CRUD ─────────────────────────────────

    /// <inheritdoc />
    public async Task<get_response_header<user_role_jurisdiction>> GetAllUserRoleJurisdictionsAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}jurisdiction/_all_docs?include_docs=true";
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<get_response_header<user_role_jurisdiction>>(response);
    }

    /// <inheritdoc />
    public async Task<user_role_jurisdiction> GetUserRoleJurisdictionAsync(string id, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}jurisdiction/{id}";
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<user_role_jurisdiction>(response);
    }

    /// <inheritdoc />
    public async Task<document_put_response> PutUserRoleJurisdictionAsync(user_role_jurisdiction item, DBConfigurationDetail dbConfig)
    {
        string objectJson = JsonConvert.SerializeObject(item, IgnoreNullSettings);
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}jurisdiction/{item._id}";
        string response = await _httpClient.ExecuteAsync("PUT", requestUrl, objectJson, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    /// <inheritdoc />
    public async Task<document_put_response> DeleteUserRoleJurisdictionAsync(string id, string rev, DBConfigurationDetail dbConfig)
    {
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}jurisdiction/{id}?rev={rev}";
        string response = await _httpClient.ExecuteAsync("DELETE", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    /// <inheritdoc />
    public async Task<List<document_put_response>> BulkUpsertUserRoleJurisdictionsAsync(
        List<user_role_jurisdiction> items,
        DBConfigurationDetail dbConfig)
    {
        string payload = JsonConvert.SerializeObject(new { docs = items }, IgnoreNullSettings);
        string requestUrl = $"{dbConfig.url}/{dbConfig.prefix}jurisdiction/_bulk_docs";
        string response = await _httpClient.ExecuteAsync("POST", requestUrl, payload, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<List<document_put_response>>(response);
    }

    // ── Sortable view queries ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<get_sortable_view_reponse_header<user_role_jurisdiction>> GetUserRoleJurisdictionSortableViewAsync(
        string requestUrl,
        DBConfigurationDetail dbConfig)
    {
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<get_sortable_view_reponse_header<user_role_jurisdiction>>(response);
    }

    /// <inheritdoc />
    public async Task<get_sortable_view_reponse_header<user_role_jurisdiction>> GetUserRoleJurisdictionSortableViewByParamsAsync(
        int skip,
        int take,
        string sortView,
        bool hasSearchKey,
        bool descending,
        DBConfigurationDetail dbConfig)
    {
        var urlBuilder = new StringBuilder();
        urlBuilder.Append(dbConfig.url);
        urlBuilder.Append($"/{dbConfig.prefix}jurisdiction/_design/sortable/_view/{sortView}?");

        if (!hasSearchKey)
        {
            urlBuilder.Append(skip > -1 ? $"skip={skip}" : "skip=0");

            if (take > -1)
            {
                urlBuilder.Append($"&limit={take}");
            }

            if (descending)
            {
                urlBuilder.Append("&descending=true");
            }
        }
        else
        {
            urlBuilder.Append("skip=0");

            if (descending)
            {
                urlBuilder.Append("&descending=true");
            }
        }

        string response = await _httpClient.ExecuteAsync("GET", urlBuilder.ToString(), null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<get_sortable_view_reponse_header<user_role_jurisdiction>>(response);
    }

    /// <inheritdoc />
    public async Task<get_sortable_view_reponse_header<session>> GetSessionSortableViewAsync(
        int skip,
        int take,
        string sortView,
        bool hasSearchKey,
        bool descending,
        DBConfigurationDetail dbConfig)
    {
        var urlBuilder = new StringBuilder();
        urlBuilder.Append(dbConfig.url);
        urlBuilder.Append($"/{dbConfig.prefix}jurisdiction/_design/sortable/_view/{sortView}?");

        if (!hasSearchKey)
        {
            urlBuilder.Append(skip > -1 ? $"skip={skip}" : "skip=0");

            if (take > -1)
            {
                urlBuilder.Append($"&limit={take}");
            }

            if (descending)
            {
                urlBuilder.Append("&descending=true");
            }
        }
        else
        {
            urlBuilder.Append("skip=0");

            if (descending)
            {
                urlBuilder.Append("&descending=true");
            }
        }

        string response = await _httpClient.ExecuteAsync("GET", urlBuilder.ToString(), null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<get_sortable_view_reponse_header<session>>(response);
    }

    // ── Jurisdiction tree document ────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<jurisdiction_tree> GetJurisdictionTreeAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<jurisdiction_tree>(response);
    }

    /// <inheritdoc />
    public async Task<document_put_response> PutJurisdictionTreeAsync(jurisdiction_tree item, DBConfigurationDetail dbConfig)
    {
        string payload = JsonConvert.SerializeObject(item, IgnoreNullSettings);
        string requestUrl = dbConfig.Get_Prefix_DB_Url("jurisdiction/jurisdiction_tree");
        string response = await _httpClient.ExecuteAsync("PUT", requestUrl, payload, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    // ── Form access list document ─────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<FormAccessSpecification> GetFormAccessAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("jurisdiction/form-access-list");
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<FormAccessSpecification>(response);
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveFormAccessAsync(FormAccessSpecification request, DBConfigurationDetail dbConfig)
    {
        string payload = JsonConvert.SerializeObject(request, IgnoreNullSettings);
        string requestUrl = dbConfig.Get_Prefix_DB_Url("jurisdiction/form-access-list");
        string response = await _httpClient.ExecuteAsync("PUT", requestUrl, payload, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    // ── Pinned case set document ──────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<pinned_case_set?> GetPinnedCaseSetAsync(DBConfigurationDetail dbConfig)
    {
        string requestUrl = dbConfig.Get_Prefix_DB_Url("jurisdiction/pinned-case-set");
        string response = await _httpClient.ExecuteAsync("GET", requestUrl, null, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<pinned_case_set>(response);
    }

    /// <inheritdoc />
    public async Task<document_put_response> SavePinnedCaseSetAsync(pinned_case_set item, DBConfigurationDetail dbConfig)
    {
        string payload = JsonConvert.SerializeObject(item, IgnoreNullSettings);
        string requestUrl = dbConfig.Get_Prefix_DB_Url("jurisdiction/pinned-case-set");
        string response = await _httpClient.ExecuteAsync("PUT", requestUrl, payload, dbConfig.user_name, dbConfig.user_value);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }
}
