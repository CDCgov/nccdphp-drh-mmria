using System;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.data.api;
using mmria.common.SharedLibraries.Queue.DAL;

namespace mmria.common.SharedLibraries.Queue.Manager;

public sealed class QueueManager
{
    private readonly QueueDAL _dal;

    public QueueManager(QueueDAL dal)
    {
        _dal = dal;
    }

    public async Task<Set_Queue_Response> SaveQueueItemAsync(
        Set_Queue_Request request,
        string authSessionValue,
        DBConfigurationDetail dbConfig)
    {
        var result = new Set_Queue_Response();
        var queueItem = new Queue_Item
        {
            queue_id = Guid.NewGuid().ToString(),
            action = request.action,
            case_list = request.case_list
        };

        try
        {
            string effectiveAuthSession = !string.IsNullOrWhiteSpace(request.security_token)
                ? request.security_token
                : authSessionValue;

            var putResponse = await _dal.SaveQueueItemAsync(queueItem, dbConfig, effectiveAuthSession);
            result.Ok = putResponse?.ok == true;
            result.Queue_Id = queueItem.queue_id;
            result.message = putResponse?.error_description;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            result.Ok = false;
            result.message = ex.ToString();
        }

        return result;
    }
}
