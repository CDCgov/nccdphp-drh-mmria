using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.Jurisdiction.Model;

namespace mmria.common.SharedLibraries.Jurisdiction.DAL;

/// <summary>
/// Thin HTTP wrapper for <c>jurisdiction/_design/sortable/_view/by_user_id</c>.
/// Implements <see cref="IJurisdictionAuthorizationReader"/> — the SQL migration seam
/// for the authorization read path.
/// No caching, no business logic, no active-role filtering.
/// </summary>
public sealed class JurisdictionAuthorizationDAL : IJurisdictionAuthorizationReader
{
    private readonly CouchDbHttpClient _httpClient;

    public JurisdictionAuthorizationDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JurisdictionRoleEntry>> GetRolesByUserIdAsync(
        string? userId,
        DBConfigurationDetail dbConfig)
    {
        var urlBuilder = new StringBuilder();
        urlBuilder.Append(dbConfig.url);
        urlBuilder.Append('/');
        urlBuilder.Append(dbConfig.prefix);
        urlBuilder.Append("jurisdiction/_design/sortable/_view/by_user_id");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            string quotedUser = $"\"{userId}\"";
            string encoded = Uri.EscapeDataString(quotedUser);
            urlBuilder.Append($"?startkey={encoded}&endkey={encoded}");
        }

        string responseJson;
        try
        {
            responseJson = await _httpClient.ExecuteAsync(
                "GET",
                urlBuilder.ToString(),
                null,
                dbConfig.user_name,
                dbConfig.user_value,
                "application/json");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(
                $"Jurisdiction auth view query failed. userId={userId ?? "<all>"}; " +
                $"prefix={dbConfig.prefix}; ex={ex.Message}");
            return Array.Empty<JurisdictionRoleEntry>();
        }

        var viewResponse = JsonConvert.DeserializeObject<
            get_sortable_view_reponse_header<user_role_jurisdiction>>(responseJson);

        if (viewResponse?.rows == null)
            return Array.Empty<JurisdictionRoleEntry>();

        return viewResponse.rows
            .Where(r => r?.value != null)
            .Select(r => new JurisdictionRoleEntry
            {
                _id = r.value._id,
                jurisdiction_id = r.value.jurisdiction_id,
                user_id = r.value.user_id,
                role_name = r.value.role_name,
                is_active = r.value.is_active,
                effective_start_date = r.value.effective_start_date,
                effective_end_date = r.value.effective_end_date
            })
            .ToList();
    }
}
