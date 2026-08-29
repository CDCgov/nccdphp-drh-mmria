#nullable enable

using System.Threading.Tasks;
using mmria.common.couchdb;

namespace mmria.common.SharedLibraries.SystemConfig;

/// <summary>
/// Repository interface for all configuration CouchDB CRUD operations.
/// SystemConfigDAL is the sole implementation. A SQL migration requires only
/// a new implementation of this interface — no caller changes needed.
/// </summary>
/// <remarks>
/// The configuration database is a single shared (non-tenant-prefixed) database.
/// URL pattern: {dbConfig.url}/configuration/{configId}
/// Never use Get_Prefix_DB_Url for configuration documents.
/// </remarks>
public interface IConfigurationRepository
{
    /// <summary>
    /// GET a configuration document as a raw JSON string.
    /// </summary>
    Task<string?> GetConfigurationJsonAsync(string configId, DBConfigurationDetail dbConfig);

    /// <summary>
    /// GET a configuration document deserialized as a ConfigurationSet.
    /// </summary>
    Task<ConfigurationSet?> GetConfigurationSetAsync(string configId, DBConfigurationDetail dbConfig, int timeoutSeconds = 20);

    /// <summary>
    /// PUT (create or update) a configuration document with the supplied JSON payload.
    /// Returns the raw response JSON from CouchDB.
    /// </summary>
    Task<string?> PutConfigurationAsync(string configId, string configJson, DBConfigurationDetail dbConfig);
}
