using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.Queue.DAL;

/// <summary>
/// Data Access Layer for all <c>queue</c> CouchDB operations.
/// Implements <see cref="IQueueRepository"/> — the SQL migration seam for the global queue database.
/// URL pattern: <c>{db_config.url}/queue/{id}</c> — no tenant prefix (Pattern A, global database).
/// No business logic — only data operations.
/// </summary>
public sealed class QueueDAL : IQueueRepository
{
    private readonly CouchDbHttpClient _httpClient;

    public QueueDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<document_put_response> SaveQueueItemAsync(
        mmria.common.data.api.Queue_Item queue_item,
        DBConfigurationDetail db_config,
        CouchDbRequestOptions requestOptions)
    {
        string queue_url = db_config.url + "/queue/" + queue_item.queue_id;
        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(queue_item);
        string response = await _httpClient.ExecuteAsync("PUT", queue_url, object_string, "application/json", requestOptions);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<document_put_response>(response);
    }
}
