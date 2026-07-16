using System.Dynamic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.ExportQueue;

/// <summary>
/// Repository interface for all export_queue database operations.
/// ExportQueueDAL is the sole implementation. A SQL/job-queue migration
/// requires only a new implementation of this interface — no caller changes needed.
/// </summary>
public interface IExportQueueRepository
{
    /// <summary>
    /// Get all documents from the export_queue database.
    /// </summary>
    Task<ExpandoObject> GetAllQueueDocumentsAsync(DBConfigurationDetail db_config);

    /// <summary>
    /// Get a single export queue document by ID, deserialized to <typeparamref name="T"/>.
    /// </summary>
    Task<T> GetQueueDocumentAsync<T>(string id, DBConfigurationDetail db_config);

    /// <summary>
    /// Save (PUT) an export queue document by ID from pre-serialized JSON.
    /// </summary>
    Task<document_put_response> SaveQueueDocumentAsync(string id, string document_content, DBConfigurationDetail db_config);

    /// <summary>
    /// Trigger the export queue service via HTTP POST.
    /// </summary>
    Task<string> TriggerExportQueueServiceAsync(
        string service_url,
        string request_json,
        string vitalServiceKey);
}
