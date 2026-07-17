using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.Queue;

/// <summary>
/// Repository interface for all <c>queue</c> database operations.
/// <see cref="DAL.QueueDAL"/> is the sole implementation.
/// The queue database uses no tenant prefix — it is a per-deployment global database (Pattern A).
/// A SQL/job-queue migration requires only a new implementation of this interface — no caller changes needed.
/// </summary>
public interface IQueueRepository
{
    /// <summary>
    /// PUT queue/{queue_item.queue_id} — saves a new queue item document.
    /// The queue database is global (no tenant prefix); auth is caller-supplied via <paramref name="requestOptions"/>.
    /// </summary>
    Task<document_put_response> SaveQueueItemAsync(
        mmria.common.data.api.Queue_Item queue_item,
        DBConfigurationDetail db_config,
        CouchDbRequestOptions requestOptions);
}
