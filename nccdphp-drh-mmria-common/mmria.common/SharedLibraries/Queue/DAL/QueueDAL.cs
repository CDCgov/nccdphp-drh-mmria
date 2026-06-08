using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.data.api;
using mmria.common.getset;
using mmria.common.model.couchdb;
using Newtonsoft.Json;

namespace mmria.common.SharedLibraries.Queue.DAL;

public sealed class QueueDAL
{
    private readonly CouchDbHttpClient _httpClient;

    public QueueDAL(CouchDbHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<document_put_response> SaveQueueItemAsync(
        Queue_Item queueItem,
        DBConfigurationDetail dbConfig,
        string authSessionValue)
    {
        string queueUrl = dbConfig.url + "/queue/" + queueItem.queue_id;
        string documentContent = JsonConvert.SerializeObject(queueItem);
        var requestOptions = new CouchDbRequestOptions();

        if (!string.IsNullOrWhiteSpace(authSessionValue))
        {
            requestOptions = new CouchDbRequestOptions
            {
                AuthSessionValue = authSessionValue
            };
        }

        string response = await _httpClient.ExecuteAsync("PUT", queueUrl, documentContent, "application/json", requestOptions);
        return JsonConvert.DeserializeObject<document_put_response>(response);
    }
}
