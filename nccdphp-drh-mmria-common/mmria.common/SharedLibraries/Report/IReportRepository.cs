using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.Report;

/// <summary>
/// Repository interface for all application-layer operations against the report database.
/// ReportDAL is the sole CouchDB implementation. A SQL migration requires only a new
/// implementation of this interface — no caller changes needed.
/// Write and lifecycle methods added in Story 24.2; read methods from Story 23.6 are unchanged.
/// </summary>
public interface IReportRepository
{
    // ── Read operations (Story 23.6) ─────────────────────────────────────────

    /// <summary>
    /// GET report/_all_docs?include_docs=true
    /// </summary>
    Task<string> GetAllReportDocumentsAsync(DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET report/_design/interactive_aggregate_report/_view/indicator_id?key="indicatorId"
    /// </summary>
    Task<string> GetIndicatorByIdAsync(string indicatorId, DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET report/_design/data_summary_view_report/_view/year_of_death?skip=N&amp;limit=N
    /// </summary>
    Task<string> GetDataSummaryViewAsync(int skip, int take, DBConfigurationDetail dbConfig);

    /// <summary>
    /// POST report/_find with the provided Mango selector JSON body.
    /// </summary>
    Task<string> FindReportDocumentsAsync(string selectorJson, DBConfigurationDetail dbConfig);

    // ── Write and lifecycle operations (Story 24.2) ──────────────────────────

    /// <summary>
    /// GET report/{id} — returns the current _rev, or null if the document does not exist.
    /// </summary>
    Task<string?> GetRevisionAsync(string id, DBConfigurationDetail dbConfig);

    /// <summary>
    /// PUT report/{id} with the supplied JObject body.
    /// The caller must set _rev on the document before calling if the document already exists.
    /// </summary>
    Task<document_put_response> UpsertDocumentAsync(string id, JObject doc, DBConfigurationDetail dbConfig);

    /// <summary>
    /// DELETE report/{id}?rev={rev}
    /// </summary>
    Task<document_put_response> DeleteDocumentAsync(string id, string rev, DBConfigurationDetail dbConfig);

    /// <summary>
    /// POST report/_bulk_docs with the supplied documents.
    /// Each document must carry _rev if it already exists in report.
    /// </summary>
    Task<IEnumerable<document_put_response>> BulkUpsertAsync(IEnumerable<JObject> docs, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Drops and recreates the report database while preserving system/config documents.
    /// Pre-fetches documents where type is "system" or "config", deletes the database,
    /// recreates it empty, then re-inserts the preserved documents.
    /// SQL equivalent: DELETE FROM report_documents WHERE type NOT IN ('system', 'config').
    /// </summary>
    Task DropAndResetWithSystemDocPreservationAsync(DBConfigurationDetail dbConfig);

    /// <summary>
    /// PUT report/_design/{designName} with the supplied design document JSON.
    /// </summary>
    Task EnsureDesignDocumentAsync(string designName, string designDocJson, DBConfigurationDetail dbConfig);

    /// <summary>
    /// POST report/_index with the supplied index JSON.
    /// </summary>
    Task EnsureIndexAsync(string indexJson, DBConfigurationDetail dbConfig);

    /// <summary>
    /// Barrier query: POST report/_find with a minimal selector to confirm index availability.
    /// Blocks until the Mango index is ready. Used by legacy sync actors before marking
    /// a rebuild complete.
    /// </summary>
    Task WaitForIndexReadyAsync(DBConfigurationDetail dbConfig);

    /// <summary>
    /// POST report/_all_docs?include_docs=false with a keys body.
    /// Returns id → rev for every document id that currently exists in report.
    /// Missing documents are omitted from the result. Used by c_document_sync_all
    /// to populate _rev before bulk writes to avoid 409 conflicts.
    /// </summary>
    Task<IDictionary<string, string>> GetRevisionBulkAsync(IEnumerable<string> ids, DBConfigurationDetail dbConfig);
}
