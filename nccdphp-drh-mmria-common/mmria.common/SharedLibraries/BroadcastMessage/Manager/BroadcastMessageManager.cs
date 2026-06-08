using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.SharedLibraries.BroadcastMessage.DAL;

namespace mmria.common.SharedLibraries.BroadcastMessage.Manager;

public sealed class BroadcastMessageManager
{
    private readonly BroadcastMessageDAL _dal;

    public BroadcastMessageManager(BroadcastMessageDAL dal)
    {
        _dal = dal;
    }

    public async Task ReplicateMessageAsync(
        OverridableConfiguration configuration,
        ConfigurationSet configDb,
        string hostPrefix,
        string objectJson)
    {
        var configUrl = configuration.GetString("vitals_url", hostPrefix).Replace("/api/Message/IJESet", "");
        var baseUrl = $"{configUrl}/api/broadcastMessage/ReplicateMessage";
        var vitalServiceKey = configDb.name_value["vital_service_key"];

        await _dal.ReplicateMessageAsync(baseUrl, objectJson, vitalServiceKey);
    }
}
