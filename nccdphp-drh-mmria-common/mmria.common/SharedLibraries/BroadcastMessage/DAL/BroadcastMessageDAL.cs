using System;
using System.Threading.Tasks;
using mmria.common.getset;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.BroadcastMessage.DAL;

public sealed class BroadcastMessageDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public BroadcastMessageDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task ReplicateMessageAsync(string baseUrl, string objectJson, string vitalServiceKey)
    {
        var requestOptions = new CouchDbRequestOptions
        {
            VitalServiceKey = vitalServiceKey
        };

        try
        {
            string responseContent = await _couchDbHttpClient.ExecuteAsync("POST", baseUrl, objectJson, "application/json", requestOptions);
            _ = System.Text.Json.JsonSerializer.Deserialize<document_put_response>(responseContent);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
