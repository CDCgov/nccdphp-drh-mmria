using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
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
                    item.storage_file_name = doc_item.ContainsKey("storage_file_name") && doc_item["storage_file_name"] != null ? doc_item["storage_file_name"].ToString() : null;
                    item.storage_directory_name = doc_item.ContainsKey("storage_directory_name") && doc_item["storage_directory_name"] != null ? doc_item["storage_directory_name"].ToString() : null;
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

    public async Task<ExportQueueDownloadResult> DownloadExportFileAsync(
        string id,
        string hostPrefix,
        string vitalsUrl,
        string vitalServiceKey,
        CancellationToken cancellationToken)
    {
        var requestUri = BuildExportDownloadUri(id, hostPrefix, vitalsUrl);
        return await _dal.DownloadExportFileAsync(requestUri, vitalServiceKey, cancellationToken);
    }

    public async Task<ExportQueueItem> GetQueueItemAsync(string id, DBConfigurationDetail db_config)
    {
        return await _dal.GetQueueDocumentAsync<ExportQueueItem>(id, db_config);
    }

    public async Task<ExportQueueItem> GetNextQueuedServiceItemAsync(DBConfigurationDetail db_config)
    {
        List<ExportQueueItem> result = await GetServiceQueueItemsAsync(
            db_config,
            requireExportType: true,
            item =>
                string.Equals(item.data_type, "export", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(item.status) &&
                item.status.StartsWith("In Queue...", StringComparison.OrdinalIgnoreCase));

        return GetOldestQueueItem(result);
    }

    public async Task<ExportQueueItem> GetNextDeletedServiceItemAsync(DBConfigurationDetail db_config)
    {
        List<ExportQueueItem> result = await GetServiceQueueItemsAsync(
            db_config,
            requireExportType: false,
            item =>
                !string.IsNullOrWhiteSpace(item.status) &&
                item.status.StartsWith("Deleted", StringComparison.OrdinalIgnoreCase));

        return GetOldestQueueItem(result);
    }

    public async Task MarkCreatingAsync(ExportQueueItem export_queue_item, DBConfigurationDetail db_config)
    {
        export_queue_item.status = "Creating Export...";
        export_queue_item.last_updated_by = "mmria-services";
        export_queue_item.date_last_updated = DateTime.Now;

        await SaveQueueItemDocumentAsync(export_queue_item, db_config);
    }

    public async Task MarkExportErrorAsync(string id, Exception exception, DBConfigurationDetail db_config)
    {
        ExportQueueItem export_queue_item = await GetQueueItemAsync(id, db_config);
        if (export_queue_item == null)
        {
            return;
        }

        var message = exception?.Message ?? "Unknown export error";
        if (message.Length > 100)
        {
            message = message.Substring(0, 100);
        }

        export_queue_item.status = $"Export error... {message}";
        export_queue_item.last_updated_by = "mmria-services";
        export_queue_item.date_last_updated = DateTime.Now;

        await SaveQueueItemDocumentAsync(export_queue_item, db_config);
    }

    public async Task MarkDownloadReadyAsync(
        string id,
        string storageFileName,
        string storageDirectoryName,
        DBConfigurationDetail db_config)
    {
        ExportQueueItem export_queue_item = await GetQueueItemAsync(id, db_config);
        if (export_queue_item == null)
        {
            return;
        }

        export_queue_item.status = "Download";
        export_queue_item.storage_file_name = storageFileName;
        export_queue_item.storage_directory_name = storageDirectoryName;

        await SaveQueueItemDocumentAsync(export_queue_item, db_config);
    }

    public async Task MarkQueueFailedAsync(string id, string error, DBConfigurationDetail db_config)
    {
        ExportQueueItem export_queue_item = await GetQueueItemAsync(id, db_config);
        if (export_queue_item == null)
        {
            return;
        }

        export_queue_item.status = "Queue Failed:" + error;

        await SaveQueueItemDocumentAsync(export_queue_item, db_config);
    }

    public async Task MarkExpungedAsync(ExportQueueItem export_queue_item, DBConfigurationDetail db_config)
    {
        export_queue_item.status = "expunged";
        export_queue_item.last_updated_by = "mmria-services";

        await SaveQueueItemDocumentAsync(export_queue_item, db_config);
    }

    public async Task<ExportQueueItem> MarkDownloadedAsync(string id, DBConfigurationDetail db_config)
    {
        ExportQueueItem export_queue_item = await GetQueueItemAsync(id, db_config);
        await MarkDownloadedAsync(export_queue_item, db_config);
        return export_queue_item;
    }

    public async Task MarkDownloadedAsync(ExportQueueItem export_queue_item, DBConfigurationDetail db_config)
    {
        export_queue_item.status = "Downloaded";

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(export_queue_item, settings);

        await _dal.SaveQueueDocumentAsync(export_queue_item._id, object_string, db_config);
    }

    private async Task SaveQueueItemDocumentAsync(ExportQueueItem export_queue_item, DBConfigurationDetail db_config)
    {
        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(export_queue_item, settings);

        await _dal.SaveQueueDocumentAsync(export_queue_item._id, object_string, db_config);
    }

    private async Task<List<ExportQueueItem>> GetServiceQueueItemsAsync(
        DBConfigurationDetail db_config,
        bool requireExportType,
        Func<ExportQueueItem, bool> predicate)
    {
        List<ExportQueueItem> result = new List<ExportQueueItem>();
        ExpandoObject response_result = await _dal.GetAllQueueDocumentsAsync(db_config);
        IDictionary<string, object> response_dictionary = response_result as IDictionary<string, object>;

        IList<object> enumerable_rows = null;
        if (response_dictionary != null && response_dictionary.ContainsKey("rows"))
        {
            enumerable_rows = response_dictionary["rows"] as IList<object>;
        }

        if (enumerable_rows == null)
        {
            return result;
        }

        foreach (IDictionary<string, object> enumerable_item in enumerable_rows)
        {
            IDictionary<string, object> doc_item =
                enumerable_item.ContainsKey("doc")
                    ? enumerable_item["doc"] as IDictionary<string, object>
                    : null;

            ExportQueueItem item = CreateQueueItemFromDocument(doc_item, requireExportType);
            if (item != null && predicate(item))
            {
                result.Add(item);
            }
        }

        return result;
    }

    private static ExportQueueItem GetOldestQueueItem(List<ExportQueueItem> items)
    {
        if (items == null || items.Count == 0)
        {
            return null;
        }

        if (items.Count > 1)
        {
            items.Sort((x, y) =>
                (x.date_created ?? DateTime.MaxValue).CompareTo(y.date_created ?? DateTime.MaxValue));
        }

        return items[0];
    }

    private static ExportQueueItem CreateQueueItemFromDocument(IDictionary<string, object> document, bool requireExportType)
    {
        var missingRequiredFields = new List<string>();
        var id = GetOptionalString(document, "_id");
        var revision = GetOptionalString(document, "_rev");
        var fileName = GetOptionalString(document, "file_name");
        var exportType = GetOptionalString(document, "export_type");

        if (string.IsNullOrWhiteSpace(id))
        {
            missingRequiredFields.Add("_id");
        }

        if (string.IsNullOrWhiteSpace(revision))
        {
            missingRequiredFields.Add("_rev");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            missingRequiredFields.Add("file_name");
        }

        if (requireExportType && string.IsNullOrWhiteSpace(exportType))
        {
            missingRequiredFields.Add("export_type");
        }

        if (missingRequiredFields.Count > 0)
        {
            LogMalformedQueueDocument(id, missingRequiredFields);
            return null;
        }

        return new ExportQueueItem
        {
            _id = id,
            _rev = revision,
            _deleted = document.ContainsKey("_deleted") ? document["_deleted"] as bool? : null,
            date_created = GetOptionalDateTime(document, "date_created"),
            created_by = GetOptionalString(document, "created_by"),
            date_last_updated = GetOptionalDateTime(document, "date_last_updated"),
            last_updated_by = GetOptionalString(document, "last_updated_by"),
            data_type = GetOptionalString(document, "data_type"),
            file_name = fileName,
            storage_file_name = GetOptionalString(document, "storage_file_name"),
            storage_directory_name = GetOptionalString(document, "storage_directory_name"),
            export_type = exportType,
            status = GetOptionalString(document, "status"),
            all_or_core = GetOptionalString(document, "all_or_core"),
            grantee_name = GetOptionalString(document, "grantee_name"),
            is_encrypted = GetOptionalString(document, "is_encrypted"),
            zip_key = GetOptionalString(document, "zip_key"),
            de_identified_selection_type = GetOptionalString(document, "de_identified_selection_type"),
            de_identified_field_set = GetOptionalStringArray(document, "de_identified_field_set", replaceHyphenWithSlash: true),
            case_filter_type = GetOptionalString(document, "case_filter_type"),
            case_file_type = GetOptionalString(document, "case_file_type"),
            case_set = GetOptionalStringArray(document, "case_set"),
            field_set = GetOptionalStringArray(document, "field_set"),
            pregnancy_relatedness = GetOptionalIntArray(document, "pregnancy_relatedness"),
            include_blank_date_of_reviews = GetOptionalBoolean(document, "include_blank_date_of_reviews"),
            include_blank_date_of_deaths = GetOptionalBoolean(document, "include_blank_date_of_deaths"),
            date_of_review_begin = GetOptionalDateTime(document, "date_of_review_begin"),
            date_of_review_end = GetOptionalDateTime(document, "date_of_review_end"),
            date_of_death_begin = GetOptionalDateTime(document, "date_of_death_begin"),
            date_of_death_end = GetOptionalDateTime(document, "date_of_death_end")
        };
    }

    private static void LogMalformedQueueDocument(string documentId, IEnumerable<string> missingRequiredFields)
    {
        System.Console.WriteLine(
            "ExportQueueManager: Skipping malformed export_queue document {0}. Missing required field(s): {1}",
            string.IsNullOrWhiteSpace(documentId) ? "(missing _id)" : documentId,
            string.Join(", ", missingRequiredFields));
    }

    private static string GetOptionalString(IDictionary<string, object> document, string key)
    {
        if (document == null || !document.ContainsKey(key) || document[key] == null)
        {
            return null;
        }

        return document[key].ToString();
    }

    private static DateTime? GetOptionalDateTime(IDictionary<string, object> document, string key)
    {
        if (document == null || !document.ContainsKey(key) || document[key] == null)
        {
            return null;
        }

        if (document[key] is DateTime dateTime)
        {
            return dateTime;
        }

        if (DateTime.TryParse(document[key].ToString(), out var parsedDateTime))
        {
            return parsedDateTime;
        }

        return null;
    }

    private static bool? GetOptionalBoolean(IDictionary<string, object> document, string key)
    {
        if (document == null || !document.ContainsKey(key) || document[key] == null)
        {
            return null;
        }

        if (document[key] is bool booleanValue)
        {
            return booleanValue;
        }

        if (bool.TryParse(document[key].ToString(), out var parsedBoolean))
        {
            return parsedBoolean;
        }

        return null;
    }

    private static string[] GetOptionalStringArray(IDictionary<string, object> document, string key, bool replaceHyphenWithSlash = false)
    {
        if (document == null || !document.ContainsKey(key) || document[key] == null)
        {
            return null;
        }

        if (document[key] is string[] stringArray)
        {
            return stringArray
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => replaceHyphenWithSlash ? item.Replace("-", "/") : item)
                .ToArray();
        }

        if (document[key] is IList<object> objectList)
        {
            return objectList
                .Where(item => item != null)
                .Select(item =>
                {
                    var value = item.ToString();
                    return replaceHyphenWithSlash ? value.Replace("-", "/") : value;
                })
                .ToArray();
        }

        return null;
    }

    private static int[] GetOptionalIntArray(IDictionary<string, object> document, string key)
    {
        if (document == null || !document.ContainsKey(key) || document[key] == null)
        {
            return null;
        }

        if (document[key] is int[] intArray)
        {
            return intArray;
        }

        if (document[key] is IList<object> objectList)
        {
            return objectList
                .Where(item => item != null && int.TryParse(item.ToString(), out var _))
                .Select(item => int.Parse(item.ToString()))
                .ToArray();
        }

        return null;
    }

    private static Uri BuildExportDownloadUri(string id, string hostPrefix, string vitalsUrl)
    {
        var servicesBaseUri = GetServicesBaseUri(vitalsUrl);
        var downloadUri = new Uri(servicesBaseUri, $"api/ExportQueue/Download/{Uri.EscapeDataString(id)}");

        var builder = new UriBuilder(downloadUri);
        var hostPrefixQuery = $"host_prefix={Uri.EscapeDataString(hostPrefix ?? string.Empty)}";
        var existingQuery = builder.Query?.TrimStart('?');
        builder.Query = string.IsNullOrWhiteSpace(existingQuery)
            ? hostPrefixQuery
            : $"{existingQuery}&{hostPrefixQuery}";

        return builder.Uri;
    }

    private static Uri GetServicesBaseUri(string vitalsUrl)
    {
        if (string.IsNullOrWhiteSpace(vitalsUrl))
        {
            throw new InvalidOperationException("The current tenant is missing vitals_url configuration.");
        }

        var servicesBaseUrl = vitalsUrl.Replace("/api/Message/IJESet", string.Empty);
        if (string.Equals(servicesBaseUrl, vitalsUrl, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The current tenant vitals_url does not contain the expected Message/IJESet path.");
        }

        if (!Uri.TryCreate(servicesBaseUrl, UriKind.Absolute, out var servicesUri))
        {
            throw new InvalidOperationException("The derived export services URL is not a valid absolute URI.");
        }

        if (servicesUri.Scheme != Uri.UriSchemeHttp && servicesUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("The derived export services URL must use HTTP or HTTPS.");
        }

        if (!string.IsNullOrWhiteSpace(servicesUri.UserInfo) || !string.IsNullOrWhiteSpace(servicesUri.Fragment))
        {
            throw new InvalidOperationException("The derived export services URL must not contain user info or fragments.");
        }

        return new UriBuilder(servicesUri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            Path = servicesUri.AbsolutePath.TrimEnd('/') + "/"
        }.Uri;
    }
}
