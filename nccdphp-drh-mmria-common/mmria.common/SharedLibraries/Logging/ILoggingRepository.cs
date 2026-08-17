using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.Logging;

/// <summary>
/// Repository interface for all logging database operations.
/// LoggingDAL is the sole implementation. A migration to SQL, Elasticsearch,
/// or another store requires only a new implementation — no caller changes needed.
/// </summary>
public interface ILoggingRepository
{
    /// <summary>
    /// GET the by-offline-session view from the logging database.
    /// Used to retrieve logging documents grouped by offline session (modules).
    /// </summary>
    Task<dynamic> GetLoggingModulesAsync(DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET a filtered view or document from the logging database using a path relative to the logging database root.
    /// The path is appended to the logging database URL via Get_Prefix_DB_Url.
    /// </summary>
    Task<string> GetFilteredLoggingAsync(string filterOrViewPath, DBConfigurationDetail dbConfig);

    /// <summary>
    /// POST a new log document to the logging database root.
    /// </summary>
    Task<document_put_response> PostLoggingDocumentAsync(string documentJson, DBConfigurationDetail dbConfig);
}
