using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.ije;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.VitalImport;

/// <summary>
/// Repository interface for all vital_import database operations.
/// VitalImportDAL is the sole implementation. A SQL migration requires
/// only a new implementation of this interface — no caller changes needed.
///
/// URL exception: vital_import is a non-tenant database. Its URL is constructed
/// as $"{dbConfig.url}/vital_import/{path}" — without Get_Prefix_DB_Url. This
/// is intentional and must not be changed to use the prefix separator.
/// </summary>
public interface IVitalImportRepository
{
    /// <summary>
    /// Get all batch documents from vital_import/_all_docs.
    /// </summary>
    Task<alldocs_response<Batch>> GetAllBatchesAsync(DBConfigurationDetail dbConfig);

    /// <summary>
    /// Save (PUT) a batch document by batch ID to vital_import/{batchId}.
    /// </summary>
    Task<document_put_response> PutBatchDocumentAsync(string batchId, string batchJson, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Save (PUT) an individual vital_import document by ID to vital_import/{id}.
    /// </summary>
    Task<document_put_response> PutVitalImportDocumentAsync(string id, string docJson, DBConfigurationDetail dbConfig);
}
