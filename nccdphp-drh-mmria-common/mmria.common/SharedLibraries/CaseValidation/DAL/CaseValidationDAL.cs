using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.CaseValidation.Model;
using mmria.common.SharedLibraries.MetadataVersion;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.CaseValidation.DAL;

public sealed class CaseValidationDAL
{
    private readonly IMetadataRepository _metadataRepository;

    public CaseValidationDAL(IMetadataRepository metadataRepository)
    {
        _metadataRepository = metadataRepository;
    }

    public async Task<CaseValidationRuleDocument> GetRuleDocumentAsync(string metadataVersion, DBConfigurationDetail dbConfig)
    {
        return await _metadataRepository.GetCaseValidationRulesAsync(metadataVersion, dbConfig);
    }

    public async Task<document_put_response> SaveRuleDocumentAsync(CaseValidationRuleDocument document, DBConfigurationDetail dbConfig)
    {
        return await _metadataRepository.SaveCaseValidationRulesAsync(document, dbConfig);
    }

    public static string CreateDocumentId(string metadataVersion)
    {
        return "case-validation-rules";
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

