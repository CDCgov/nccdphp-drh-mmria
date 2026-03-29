using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.ExportQueue.DAL;
using mmria.common.SharedLibraries.ExportQueue.Model;

namespace mmria.common.SharedLibraries.ExportQueue.Manager;

public sealed class ExportQueueManager
{
    private readonly ExportQueueDAL _dal;

    public ExportQueueManager(ExportQueueDAL dal)
    {
        _dal = dal;
    }

    public async Task<IEnumerable<ExportQueueItem>> GetQueueItemsForUserAsync(string userName, DBConfigurationDetail db_config)
    {
        List<ExportQueueItem> result = new List<ExportQueueItem>();
        ExpandoObject response_result = await _dal.GetAllQueueDocumentsAsync(db_config);
        IDictionary<string, object> response_dictionary = response_result as IDictionary<string, object>;

        IList<object> enumerable_rows = null;
        if (response_dictionary != null && response_dictionary.ContainsKey("rows"))
        {
            enumerable_rows = response_dictionary["rows"] as IList<object>;
        }

        if (enumerable_rows != null)
        {
            foreach (IDictionary<string, object> enumerable_item in enumerable_rows)
            {
                IDictionary<string, object> doc_item = enumerable_item["doc"] as IDictionary<string, object>;

                if (doc_item == null)
                {
                    continue;
                }

                ExportQueueItem item = new ExportQueueItem();
                try
                {
                    item._id = doc_item["_id"].ToString();
                    item._rev = doc_item["_rev"].ToString();
                    item._deleted = doc_item.ContainsKey("_deleted") ? doc_item["_deleted"] as bool? : null;
                    item.date_created = doc_item["date_created"] as DateTime?;
                    item.created_by = doc_item.ContainsKey("created_by") ? doc_item["created_by"] as string : null;
                    item.date_last_updated = doc_item["date_last_updated"] as DateTime?;
                    item.last_updated_by = doc_item.ContainsKey("last_updated_by") ? doc_item["last_updated_by"] as string : null;
                    item.file_name = doc_item["file_name"] != null ? doc_item["file_name"].ToString() : null;
                    item.export_type = doc_item["export_type"] != null ? doc_item["export_type"].ToString() : null;
                    item.status = doc_item["status"] != null ? doc_item["status"].ToString() : null;

                    if (userName.ToLowerInvariant() == item.created_by.ToLowerInvariant())
                    {
                        result.Add(item);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        return result;
    }

    public async Task<document_put_response> SaveQueueItemAsync(
        ExportQueueItem queue_item,
        string userName,
        DBConfigurationDetail db_config)
    {
        document_put_response result = new document_put_response();

        var is_match = Regex.IsMatch(queue_item._id, @"^\d\d\d\d-\d\d-\d\dT\d\d-\d\d-\d\d.\d\d\dZ.zip$");

        if (!is_match || queue_item == null)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(queue_item.created_by))
        {
            queue_item.created_by = userName;
        }

        queue_item.last_updated_by = userName;

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(queue_item, settings);

        result = await _dal.SaveQueueDocumentAsync(queue_item._id, object_string, db_config);
        return result;
    }

    public bool ShouldTriggerService(ExportQueueItem queue_item, document_put_response result)
    {
        return result.ok &&
               (
                   queue_item.status.StartsWith("In Queue...", StringComparison.OrdinalIgnoreCase) ||
                   queue_item.status.StartsWith("Deleted", StringComparison.OrdinalIgnoreCase)
               );
    }

    public async Task TriggerExportQueueServiceAsync(
        ExportQueueItem queue_item,
        string jurisdiction_user_name,
        string host_prefix,
        string vitals_url,
        string vital_service_key)
    {
        string user_db_url = vitals_url.Replace("Message/IJESet", "ExportQueue");

        var requestBody = new
        {
            queue_item_id = queue_item._id,
            jurisdiction_user_name,
            host_prefix
        };

        string requestJson = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
        await _dal.TriggerExportQueueServiceAsync(
            user_db_url,
            requestJson,
            vital_service_key);
    }

    public async Task<ExportQueueItem> MarkDownloadedAsync(string id, DBConfigurationDetail db_config)
    {
        ExportQueueItem export_queue_item = await _dal.GetQueueDocumentAsync<ExportQueueItem>(id, db_config);

        export_queue_item.status = "Downloaded";

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(export_queue_item, settings);

        await _dal.SaveQueueDocumentAsync(export_queue_item._id, object_string, db_config);
        return export_queue_item;
    }
}
