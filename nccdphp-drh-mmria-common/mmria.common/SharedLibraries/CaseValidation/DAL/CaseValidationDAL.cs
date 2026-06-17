using System;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.CaseValidation.Model;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.CaseValidation.DAL;

public sealed class CaseValidationDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public CaseValidationDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<CaseValidationRuleDocument> GetRuleDocumentAsync(string metadataVersion, DBConfigurationDetail dbConfig)
    {
        try
        {
            var response = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                $"{dbConfig.url}/metadata/{CreateDocumentId(metadataVersion)}",
                null,
                dbConfig.user_name,
                dbConfig.user_value);

            return JsonConvert.DeserializeObject<CaseValidationRuleDocument>(response, CreateSerializerSettings());
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("(404) Object Not Found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("not_found", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            throw;
        }
    }

    public async Task<document_put_response> SaveRuleDocumentAsync(CaseValidationRuleDocument document, DBConfigurationDetail dbConfig)
    {
        var json = JsonConvert.SerializeObject(document, CreateSerializerSettings());
        var response = await _couchDbHttpClient.ExecuteAsync(
            "PUT",
            $"{dbConfig.url}/metadata/{document._id}",
            json,
            dbConfig.user_name,
            dbConfig.user_value);

        return JsonConvert.DeserializeObject<document_put_response>(response);
    }

    public static string CreateDocumentId(string metadataVersion)
    {
        return $"case-validation-rules-{metadataVersion}";
    }

    public static JsonSerializerSettings CreateSerializerSettings()
    {
        return new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
    }
}

