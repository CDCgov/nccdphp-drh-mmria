using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.DeIdentified;

/// <summary>
/// Repository interface covering all de_id database operations required by
/// sync, rebuild, and lifecycle actors. DeIdentifiedDAL is the sole CouchDB
/// implementation. A SQL migration requires only a new implementation of this
/// interface — no caller changes needed.
/// </summary>
public interface IDeIdentifiedRepository
{
    /// <summary>
    /// GET de_id/{id} — returns the current _rev, or null if the document does not exist.
    /// </summary>
    Task<string?> GetRevisionAsync(string id, DBConfigurationDetail dbConfig);

    /// <summary>
    /// PUT de_id/{id} with the supplied JObject body.
    /// The caller must set _rev on the document before calling if the document already exists.
    /// </summary>
    Task<document_put_response> UpsertDocumentAsync(string id, JObject doc, DBConfigurationDetail dbConfig);

    /// <summary>
    /// DELETE de_id/{id}?rev={rev}
    /// </summary>
    Task<document_put_response> DeleteDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig);

    /// <summary>
    /// POST de_id/_bulk_docs with the supplied documents.
    /// Each document must carry _rev if it already exists in de_id.
    /// </summary>
    Task<IEnumerable<document_put_response>> BulkUpsertAsync(IEnumerable<JObject> docs, DBConfigurationDetail dbConfig);

    /// <summary>
    /// DELETE the de_id database then PUT it empty.
    /// SQL equivalent: TRUNCATE TABLE de_id.
    /// </summary>
    Task DropAndResetAsync(DBConfigurationDetail dbConfig);

    /// <summary>
    /// PUT de_id/_design/{designName} with the supplied design document JSON.
    /// </summary>
    Task EnsureDesignDocumentAsync(string designName, string designDocJson, DBConfigurationDetail dbConfig);

    /// <summary>
    /// POST de_id/_index with the supplied index JSON.
    /// </summary>
    Task EnsureIndexAsync(string indexJson, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Barrier query: GET de_id/_design/sortable/_view/by_date_created?limit=1&amp;update=true.
    /// Blocks until the sortable index build is complete. Used by rebuild orchestrators
    /// to confirm index availability before marking the rebuild complete.
    /// </summary>
    Task WaitForIndexReadyAsync(DBConfigurationDetail dbConfig);

    /// <summary>
    /// POST de_id/_all_docs?include_docs=false with a keys body.
    /// Returns id → rev for every document id that currently exists in de_id.
    /// Missing documents are omitted from the result. Used by c_document_sync_all
    /// to populate _rev before bulk writes to avoid 409 conflicts.
    /// </summary>
    Task<IDictionary<string, string>> GetRevisionBulkAsync(IEnumerable<string> ids, DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET de_id/{id} — returns the full document JSON, or null if not found.
    /// </summary>
    Task<string?> GetDocumentJsonAsync(string id, DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET de_id/_all_docs?include_docs={includeDocs} — returns all documents as raw JSON.
    /// </summary>
    Task<string> GetAllDocumentsJsonAsync(bool includeDocs, DBConfigurationDetail dbConfig);
}
