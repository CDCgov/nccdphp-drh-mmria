using mmria.common.SharedLibraries.MMRIAServices.DAL;
using mmria.common.SharedLibraries.MMRIAServices.Helper;
using mmria.common.SharedLibraries.MMRIAServices.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace mmria.common.SharedLibraries.MMRIAServices.Manager;

public sealed class MMRIAServicesManager
{
    private readonly MMRIAServicesDAL _mmriaServicesDal;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly System.Net.Http.HttpClient _externalHttpClient;

    public MMRIAServicesManager(MMRIAServicesDAL mmriaServicesDal, mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        _mmriaServicesDal = mmriaServicesDal ?? throw new ArgumentNullException(nameof(mmriaServicesDal));
        _couchDbHttpClient = couchDbHttpClient ?? throw new ArgumentNullException(nameof(couchDbHttpClient));
        var httpClientFactory = new mmria.common.SimpleHttpClientFactory();
        _externalHttpClient = httpClientFactory.CreateClient("external");
    }

    public mmria.common.couchdb.ConfigurationSet GetConfiguration(
        string couchDbUrl,
        string configId,
        string userName,
        string password
    )
    {
        string configurationDocumentJson = _mmriaServicesDal.GetConfigurationDocumentJson(
            couchDbUrl,
            configId,
            userName,
            password
        );

        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.couchdb.ConfigurationSet>(configurationDocumentJson);

        if
        (
            result != null &&
            result.name_value.ContainsKey("metadata_version")
        )
        {
            System.Console.WriteLine($"metadata version: {result.name_value["metadata_version"]}");
        }

        return result ?? new mmria.common.couchdb.ConfigurationSet();
    }

    public static string BuildTenantDatabaseCountsServiceUrl(string vitalsUrl)
    {
        return new Uri(BuildValidatedServicesBaseUri(vitalsUrl), "api/TenantDatabaseCounts").AbsoluteUri;
    }

    public async Task<TenantDatabaseCountsResponse> GetTenantDatabaseCountsFromServiceAsync(
        string vitalsUrl,
        string vitalServiceKey)
    {
        string serviceUrl = BuildTenantDatabaseCountsServiceUrl(vitalsUrl);
        return await _mmriaServicesDal.GetTenantDatabaseCountsFromServiceAsync(serviceUrl, vitalServiceKey);
    }

    public async Task<TenantDatabaseCountsResponse> GetTenantDatabaseCountsAsync(
        mmria.common.couchdb.ConfigurationSet runtimeConfigSet,
        string configId = null,
        int maxConcurrentEntries = 4,
        int perDatabaseTimeoutSeconds = 20)
    {
        var cdcConfigurationAccess = ResolveCdcConfigurationAccess(runtimeConfigSet);
        string resolvedConfigId = string.IsNullOrWhiteSpace(configId)
            ? cdcConfigurationAccess.configId
            : configId;

        var configuration = await _mmriaServicesDal.GetConfigurationDocumentAsync(
            cdcConfigurationAccess.dbConfig,
            resolvedConfigId,
            timeoutSeconds: perDatabaseTimeoutSeconds);

        if (configuration == null)
        {
            throw new InvalidOperationException($"Configuration document '{resolvedConfigId}' could not be loaded.");
        }

        return await BuildTenantDatabaseCountsResponseAsync(
            configuration,
            maxConcurrentEntries,
            perDatabaseTimeoutSeconds);
    }

    public async Task<TenantDatabaseCountsResponse> GetTenantDatabaseCountsFromCdcConfigDbAsync(
        mmria.common.couchdb.DBConfigurationDetail cdcConfigDb,
        int maxConcurrentEntries = 4,
        int perDatabaseTimeoutSeconds = 20)
    {
        if (cdcConfigDb == null)
        {
            throw new ArgumentNullException(nameof(cdcConfigDb));
        }

        var configuration = await _mmriaServicesDal.GetConfigurationDocumentAsync(
            cdcConfigDb,
            "cdc",
            timeoutSeconds: perDatabaseTimeoutSeconds);

        if (configuration == null)
        {
            throw new InvalidOperationException("Configuration document 'cdc' could not be loaded.");
        }

        return await BuildTenantDatabaseCountsResponseAsync(
            configuration,
            maxConcurrentEntries,
            perDatabaseTimeoutSeconds);
    }

    public async Task<TenantDatabaseCountsResponse> GetTenantDatabaseCountsFromConfigurationAsync(
        mmria.common.couchdb.ConfigurationSet configuration,
        int maxConcurrentEntries = 4,
        int perDatabaseTimeoutSeconds = 20)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return await BuildTenantDatabaseCountsResponseAsync(
            configuration,
            maxConcurrentEntries,
            perDatabaseTimeoutSeconds);
    }

    private async Task<TenantDatabaseCountsResponse> BuildTenantDatabaseCountsResponseAsync(
        mmria.common.couchdb.ConfigurationSet configuration,
        int maxConcurrentEntries,
        int perDatabaseTimeoutSeconds)
    {
        var response = new TenantDatabaseCountsResponse
        {
            configuration_id = configuration._id,
            generated_utc = DateTime.UtcNow
        };

        var detailEntries = configuration.detail_list?
            .Where(kvp => !string.Equals(kvp.Key, "vital_import", StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new KeyValuePair<string, mmria.common.couchdb.DBConfigurationDetail>(kvp.Key, kvp.Value))
            .ToList() ?? new List<KeyValuePair<string, mmria.common.couchdb.DBConfigurationDetail>>();

        var semaphore = new SemaphoreSlim(Math.Max(1, maxConcurrentEntries));
        var results = new ConcurrentBag<TenantDatabaseCountEntryResponse>();

        var tasks = detailEntries.Select(async kvp =>
        {
            await semaphore.WaitAsync();
            try
            {
                results.Add(await BuildTenantDatabaseCountEntryAsync(
                    kvp.Key,
                    kvp.Value,
                    perDatabaseTimeoutSeconds));
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        response.entries = results
            .OrderBy(item => item.entry_name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        response.total_entry_count = response.entries.Count;
        response.ok_entry_count = response.entries.Count(item => string.Equals(item.status, "ok", StringComparison.OrdinalIgnoreCase));
        response.partial_error_entry_count = response.entries.Count(item => string.Equals(item.status, "partial_error", StringComparison.OrdinalIgnoreCase));
        response.error_entry_count = response.entries.Count(item => string.Equals(item.status, "error", StringComparison.OrdinalIgnoreCase));

        return response;
    }

    public async System.Threading.Tasks.Task<string> PingCVSServer
    (
        mmria.common.couchdb.ConfigurationSet ConfigDB
    )
    {
        var response_string = "";
        try
        {
            var base_url = ConfigDB.name_value["cvs_api_url"];

            var sever_status_body = new mmria.common.cvs.server_status_post_body()
            {
                id = ConfigDB.name_value["cvs_api_id"],
                secret = ConfigDB.name_value["cvs_api_key"],

            };

            var body_text = System.Text.Json.JsonSerializer.Serialize(sever_status_body);

            var content = new System.Net.Http.StringContent(body_text, System.Text.Encoding.UTF8, "application/json");
            var response = await _externalHttpClient.PostAsync(base_url, content);
            response_string = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine(response_string);

        }
        catch (System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController  POST\n{ex}");

            /*return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: ex.Message,
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );*/
        }
        //"Server is up!"


        return response_string.Trim('"');
    }

    public async Task<mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch>> GetBatchSet(
        string couchdb_url,
        string timer_user_name,
        string timer_value
    )
    {
        return await _mmriaServicesDal.GetBatchSet(couchdb_url, timer_user_name, timer_value);
    }

    public async Task<(bool result, mmria.common.ije.Batch updated_batch)> save_batch(
        mmria.common.ije.Batch p_batch,
        mmria.common.ije.Batch current_batch,
        string couchdb_url,
        string timer_user_name,
        string timer_value
    )
    {
        bool result = false;
        var updated_batch = current_batch;

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(current_batch, settings);


        try
        {
            var put_result = await _mmriaServicesDal.SaveBatchDocument(
                couchdb_url,
                p_batch.id,
                object_string,
                timer_user_name,
                timer_value
            );

            if (put_result.ok)
            {
                result = true;

                var new_batch = new mmria.common.ije.Batch()
                {
                    id = current_batch.id,
                    _rev = put_result.rev,
                    date_created = current_batch.date_created,
                    created_by = current_batch.created_by,
                    date_last_updated = DateTime.UtcNow,
                    last_updated_by = current_batch.last_updated_by,
                    Status = p_batch.Status,
                    reporting_state = current_batch.reporting_state,
                    ImportDate = current_batch.ImportDate,
                    mor_file_name = current_batch.mor_file_name,
                    nat_file_name = current_batch.nat_file_name,
                    fet_file_name = current_batch.fet_file_name,
                    StatusInfo = p_batch.StatusInfo,
                    record_result = p_batch.record_result

                };

                updated_batch = new_batch;
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return (result, updated_batch);
    }


    public async Task<mmria.common.ije.Batch> Get_batch(
        string couchdb_url,
        string timer_user_name,
        string timer_value,
        string _id
    )
    {
        mmria.common.ije.Batch result = null;


        try
        {
            result = await _mmriaServicesDal.Get_batch(
                couchdb_url,
                timer_user_name,
                timer_value,
                _id
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }


    public async Task<bool> delete_batch_document(
        string couchdb_url,
        string timer_user_name,
        string timer_value,
        string _id
    )
    {
        bool result = false;

        var batch = await Get_batch(couchdb_url, timer_user_name, timer_value, _id);


        try
        {
            result = await _mmriaServicesDal.delete_batch_document(
                couchdb_url,
                timer_user_name,
                timer_value,
                _id,
                batch._rev
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    public async Task<System.Dynamic.ExpandoObject> GetCaseById(mmria.common.couchdb.DBConfigurationDetail db_info, string case_id)
    {
        try
        {
            return await _mmriaServicesDal.GetCaseById(db_info, case_id);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return null;
    }

    public async Task<HashSet<string>> GetExistingRecordIds(mmria.common.couchdb.DBConfigurationDetail item_db_info)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        try
        {
            result = await _mmriaServicesDal.GetExistingRecordIds(item_db_info);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetExistingRecordIds: {ex}");
        }

        return result;
    }

    public async Task<(bool duplicate_is_found, Dictionary<string, int> duplicate_count)> CheckForVitalImportBatchDuplicates(
        string[] mor_set,
        int mor_max_length,
        DateTime ImportDate,
        string mor_file_name,
        string ReportingState,
        mmria.common.couchdb.DBConfigurationDetail item_db_info,
        Dictionary<string, (string, mmria.common.ije.BatchItem)> batch_item_set,
        HashSet<string> g_cdc_identifier_set)
    {
        var duplicate_count = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var duplicate_is_found = false;

        HashSet<string> ExistingRecordIds = null;
        if (ExistingRecordIds == null)
        {
            Console.WriteLine("Getting existing record IDs");
            ExistingRecordIds = await GetExistingRecordIds(item_db_info);
            Console.WriteLine($"Found {ExistingRecordIds?.Count ?? 0} existing records");
        }

        Console.WriteLine("Processing MOR records");
        foreach (var row in mor_set)
        {
            if (row.Length == mor_max_length)
            {
                var batch_item = Helper.MMRIAServicesHelper.ConvertLineToBatchItem(row, ImportDate, mor_file_name, ReportingState, ExistingRecordIds);

                if (batch_item_set.ContainsKey(batch_item.CDCUniqueID))
                {
                    duplicate_is_found = true;
                    duplicate_count[batch_item.CDCUniqueID] += 1;
                    continue;
                }

                g_cdc_identifier_set.Add(batch_item.CDCUniqueID?.Trim());

                batch_item_set.Add(batch_item.CDCUniqueID?.Trim(), (row, batch_item));
                duplicate_count[batch_item.CDCUniqueID] = 1;
            }
        }

        return (duplicate_is_found, duplicate_count);
    }

    public async Task<(bool is_case_already_present, string mmria_id, string record_id)> IsCaseAlreadyPresent(
        mmria.common.couchdb.DBConfigurationDetail item_db_info,
        string host_state,
        Dictionary<string, string> mor_field_set,
        Dictionary<string, string> ije_to_mmria_path
    )
    {
        var is_case_already_present = false;
        string mmria_id = null;
        string record_id = null;

        var case_view_response = await _mmriaServicesDal.GetCaseView(item_db_info, mor_field_set["LNAME"].Trim());

        var gs = new migrate.C_Get_Set_Value(new System.Text.StringBuilder());

        if (case_view_response != null && case_view_response.total_rows > 0)
        {
            int dod_yr = -1;
            int dod_mo = -1;
            int dod_dy = -1;

            int dob_yr = -1;
            int dob_mo = -1;
            int dob_dy = -1;

            int.TryParse(mor_field_set["DOD_YR"], out dod_yr);
            int.TryParse(mor_field_set["DOD_MO"], out dod_mo);
            int.TryParse(mor_field_set["DOD_DY"], out dod_dy);

            int.TryParse(mor_field_set["DOB_YR"], out dob_yr);
            int.TryParse(mor_field_set["DOB_MO"], out dob_mo);
            int.TryParse(mor_field_set["DOB_DY"], out dob_dy);



            foreach (var kvp in case_view_response.rows)
            {


                if
                (
                    kvp.value.host_state.Trim().Equals(host_state.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    kvp.value.last_name.Trim().Equals(mor_field_set["LNAME"].Trim(), StringComparison.OrdinalIgnoreCase) &&
                    kvp.value.first_name.Trim().Equals(mor_field_set["GNAME"].Trim(), StringComparison.OrdinalIgnoreCase) &&
                    kvp.value.date_of_death_year == dod_yr &&
                    kvp.value.date_of_death_month == dod_mo

                )
                {
                    var case_expando_object = await _mmriaServicesDal.GetCaseById(item_db_info, kvp.id);
                    if (case_expando_object != null)
                    {

                        migrate.C_Get_Set_Value.get_value_result value_result = gs.get_value(case_expando_object, "_id");
                        mmria_id = value_result.result?.ToString();


                        var DSTATE_result = gs.get_value(case_expando_object, ije_to_mmria_path["DState"]);
                        var host_state_result = gs.get_value(case_expando_object, "host_state");
                        var DOD_YR_result = gs.get_value(case_expando_object, ije_to_mmria_path["DOD_YR"]);
                        var DOD_MO_result = gs.get_value(case_expando_object, ije_to_mmria_path["DOD_MO"]);
                        var DOD_DY_result = gs.get_value(case_expando_object, ije_to_mmria_path["DOD_DY"]);
                        var DOB_YR_result = gs.get_value(case_expando_object, ije_to_mmria_path["DOB_YR"]);
                        var DOB_MO_result = gs.get_value(case_expando_object, ije_to_mmria_path["DOB_MO"]);
                        var DOB_DY_result = gs.get_value(case_expando_object, ije_to_mmria_path["DOB_DY"]);
                        var LNAME_result = gs.get_value(case_expando_object, ije_to_mmria_path["LNAME"]);
                        var GNAME_result = gs.get_value(case_expando_object, ije_to_mmria_path["GNAME"]);

                        if
                        (
                            DOD_YR_result.is_error == false &&
                            host_state_result.is_error == false &&
                            DOD_MO_result.is_error == false &&
                            DOD_DY_result.is_error == false &&
                            DOB_YR_result.is_error == false &&
                            DOB_MO_result.is_error == false &&
                            DOB_DY_result.is_error == false &&
                            LNAME_result.is_error == false &&
                            GNAME_result.is_error == false
                        )
                        {
                            var host_state_string = host_state_result.result?.ToString().Trim() ?? "";
                            var LNAME_string = LNAME_result.result?.ToString().Trim() ?? "";
                            var GNAME_string = GNAME_result.result?.ToString().Trim() ?? "";

                            if
                            (
                                host_state_string.Equals(host_state, StringComparison.OrdinalIgnoreCase) &&
                                LNAME_string.Equals(mor_field_set["LNAME"].Trim(), StringComparison.OrdinalIgnoreCase) &&
                                GNAME_string.Equals(mor_field_set["GNAME"].Trim(), StringComparison.OrdinalIgnoreCase) &&
                                DOD_YR_result.result!= null &&
                                DOD_MO_result.result!= null &&
                                DOD_DY_result.result!= null &&
                                DOB_YR_result.result!= null &&
                                DOB_MO_result.result!= null &&
                                DOB_DY_result.result!= null


                            )
                            {

                                int DOD_YR_result_Check = -1;
                                int DOD_MO_result_Check = -1;
                                int DOD_DY_result_Check = -1;
                                int DOB_YR_result_Check = -1;
                                int DOB_MO_result_Check = -1;
                                int DOB_DY_result_Check = -1;



                                if(
                                    int.TryParse(DOD_YR_result.result.ToString(), out DOD_YR_result_Check) &&
                                    int.TryParse(DOD_MO_result.result.ToString(), out DOD_MO_result_Check) &&
                                    int.TryParse(DOD_DY_result.result.ToString(), out DOD_DY_result_Check) &&
                                    int.TryParse(DOB_YR_result.result.ToString(), out DOB_YR_result_Check) &&
                                    int.TryParse(DOB_MO_result.result.ToString(), out DOB_MO_result_Check) &&
                                    int.TryParse(DOB_DY_result.result.ToString(), out DOB_DY_result_Check) &&
                                    DOD_YR_result_Check == dod_yr &&
                                    DOD_MO_result_Check == dod_mo &&
                                    DOD_DY_result_Check == dod_dy &&
                                    DOB_YR_result_Check == dob_yr &&
                                    DOB_MO_result_Check == dob_mo &&
                                    DOB_DY_result_Check == dob_dy
                                )
                                {
                                    var record_id_result = gs.get_value(case_expando_object, "home_record/record_id");
                                    if(!record_id_result.is_error && record_id_result.result!= null)
                                    {
                                        record_id = record_id_result.result.ToString();
                                    }
                                    is_case_already_present = true;
                                    break;
                                }
                                else
                                {
                                    System.Console.WriteLine("inner check 5");
                                }
                            }
                            else
                            {
                                System.Console.WriteLine("inner check 4");
                            }
                        }
                        else
                        {
                            System.Console.WriteLine("inner check 3");
                        }

                    }
                    else
                    {
                        System.Console.WriteLine("inner check 2");
                    }
                }
                else
                {
                    System.Console.WriteLine("inner check 1");
                }
            }

        }
        else
        {
            System.Console.WriteLine("No CaseView Rows found");
        }

        return (is_case_already_present, mmria_id, record_id);
    }

    public async Task<(string Name, string Description)> PopulateCDCInstanceManger(
        mmria.common.metadata.Populate_CDC_Instance message,
        mmria.common.couchdb.ConfigurationSet db_config_set,
        Action<string> progressCallback = null,
        PopulateCdcThrottleSettings throttleSettings = null
    )
    {
        throttleSettings ??= PopulateCdcThrottleSettings.CreateDefaults();
        var copy_settings = throttleSettings.Copy ?? PopulateCdcThrottleSettings.CreateDefaults().Copy;

        if (!db_config_set.detail_list.ContainsKey("cdc") && !db_config_set.detail_list.ContainsKey("cdcqa"))
        {
            throw new Exception("Exception: db_config_set.detail_list.key missing for cdc");
        }

        if (!db_config_set.name_value.ContainsKey("metadata_version"))
        {
            throw new Exception("Exception: db_config_set.name_value_key missing for metadata_version");
        }

        string metadata_release_version_name = db_config_set.name_value["metadata_version"];
        var cdc_connection = db_config_set.detail_list.ContainsKey("cdc") ? db_config_set.detail_list["cdc"] : db_config_set.detail_list["cdcqa"];
        Dictionary<string, HashSet<string>> deIdentifiedExportPathMap = null;
        int totalCaseCount = 0;
        int selectedSourceCount = message.state_list.Count(item =>
            item?.is_included == true &&
            !string.IsNullOrWhiteSpace(item.prefix) &&
            db_config_set.detail_list.ContainsKey(item.prefix));
        int processedSourceCount = 0;
        int sourceErrorCount = 0;
        int bulkWriteErrorCount = 0;

        Console.WriteLine(
            $"[PopulateCDC] Starting populate run. metadata version: {metadata_release_version_name}, " +
            $"target: {cdc_connection.url}. Copy throttling: {copy_settings.ToLogString()}.");
        progressCallback?.Invoke($"Phase 1 of 2: preparing CDC transfer for {selectedSourceCount} selected jurisdictions.");
        await SetupPopulateCdcDatabases(cdc_connection);

        for (var i = 0; i < message.state_list.Count; i++)
        {
            if (message.state_list[i].is_included != true)
            {
                continue;
            }

            var instance_name = message.state_list[i].prefix;
            var sourceDatabaseLabel = FormatPopulateCdcSourceDatabaseLabel(instance_name);
            if (!db_config_set.detail_list.ContainsKey(instance_name))
            {
                continue;
            }

            try
            {
                int currentSourceNumber = processedSourceCount + 1;
                deIdentifiedExportPathMap ??= await _mmriaServicesDal.GetDeIdentifiedExportListPathMapAsync(cdc_connection);
                var db_info = db_config_set.detail_list[instance_name];
                var caseIds = await GetPopulateCdcCaseIds(db_info);
                var resolvedDeIdentifiedPaths = ResolvePopulateCdcDeIdentifiedPaths(deIdentifiedExportPathMap, instance_name);
                Console.WriteLine($"[PopulateCDC] Jurisdiction '{instance_name}' has {caseIds.Count} cases to copy.");
                progressCallback?.Invoke($"Phase 1 of 2: jurisdiction {instance_name} ({currentSourceNumber} of {selectedSourceCount}). {sourceDatabaseLabel} has {caseIds.Count} cases. Copied {totalCaseCount} CDC case documents so far.");

                if (caseIds.Count == 0)
                {
                    processedSourceCount++;
                    continue;
                }

                var caseIdList = caseIds.ToList();
                int sourceCopiedCount = 0;
                for (int index = 0; index < caseIdList.Count; index += copy_settings.PageSize)
                {
                    int batchNumber = (index / copy_settings.PageSize) + 1;
                    var caseBatchIds = caseIdList.Skip(index).Take(copy_settings.PageSize).ToList();
                    var caseBatch = await GetPopulateCdcCaseDocuments(db_info, caseBatchIds);
                    var documentsToSave = new ConcurrentBag<string>();

                    await Parallel.ForEachAsync(
                        caseBatch,
                        new ParallelOptions { MaxDegreeOfParallelism = copy_settings.MaxParallelism },
                        async (case_row, cancellation_token) =>
                        {
                            var case_doc = case_row as IDictionary<string, object>;
                            if
                            (
                                case_doc == null ||
                                !case_doc.ContainsKey("_id") ||
                                case_doc["_id"] == null ||
                                case_doc["_id"].ToString().StartsWith("_design", StringComparison.InvariantCultureIgnoreCase)
                            )
                            {
                                return;
                            }

                            var document_json = Newtonsoft.Json.JsonConvert.SerializeObject(case_doc);
                            var de_identified_json = await DeIdentifyCaseForPopulateCDC(
                                document_json,
                                instance_name,
                                cdc_connection,
                                metadata_release_version_name,
                                resolvedDeIdentifiedPaths
                            );

                            var de_identified_case = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(de_identified_json);
                            case_doc["_rev"] = null;

                            var de_identified_dictionary = de_identified_case as IDictionary<string, object>;
                            if (de_identified_dictionary == null)
                            {
                                return;
                            }

                            ClearPopulateCdcLockFields(de_identified_dictionary);

                            var save_json = Newtonsoft.Json.JsonConvert.SerializeObject(de_identified_dictionary);
                            documentsToSave.Add(save_json);
                        });

                    var save_document_list = documentsToSave.ToList();

                    if (save_document_list.Count > 0)
                    {
                        var write_result = await BulkSavePopulateCdcDocumentsWithThrottle(
                            save_document_list,
                            cdc_connection,
                            copy_settings);
                        totalCaseCount += write_result.success_count;
                        sourceCopiedCount += write_result.success_count;
                        bulkWriteErrorCount += write_result.error_count;
                    }

                    Console.WriteLine(
                        $"[PopulateCDC] Jurisdiction '{instance_name}' batch {batchNumber}: fetched {caseBatch.Count} cases " +
                        $"and wrote {save_document_list.Count} CDC mmrds docs. Bulk write errors so far: {bulkWriteErrorCount}.");
                    progressCallback?.Invoke(
                        $"Phase 1 of 2: copied {totalCaseCount} CDC case documents so far. " +
                        $"Current jurisdiction {instance_name} ({currentSourceNumber} of {selectedSourceCount}): " +
                        $"{sourceCopiedCount} of {caseIdList.Count} cases copied from the {sourceDatabaseLabel}. " +
                        $"CDC case database bulk errors: {bulkWriteErrorCount}.");

                    bool has_more_source_batches = index + copy_settings.PageSize < caseIdList.Count;
                    if (has_more_source_batches && copy_settings.BatchDelayMs > 0)
                    {
                        await Task.Delay(copy_settings.BatchDelayMs, cancellationToken: CancellationToken.None);
                    }
                }

                processedSourceCount++;
                progressCallback?.Invoke(
                    $"Phase 1 of 2: completed jurisdiction {instance_name} ({processedSourceCount} of {selectedSourceCount}). " +
                    $"Copied {totalCaseCount} CDC case documents so far. CDC case database bulk errors: {bulkWriteErrorCount}.");
            }
            catch (Exception ex)
            {
                sourceErrorCount++;
                processedSourceCount++;
                Console.WriteLine($"Problem pulling instance:{instance_name}");
                Console.WriteLine(ex);
                progressCallback?.Invoke($"Phase 1 of 2: jurisdiction {instance_name} ({processedSourceCount} of {selectedSourceCount}) failed. Copied {totalCaseCount} CDC case documents so far. Jurisdiction errors: {sourceErrorCount}.");
            }
        }

        Console.WriteLine(
            $"[PopulateCDC] Populate run complete. Wrote {totalCaseCount} CDC mmrds documents to {cdc_connection.url}/mmrds. " +
            $"CDC mmrds bulk write errors: {bulkWriteErrorCount}.");
        progressCallback?.Invoke(
            $"Phase 1 of 2 complete. Wrote {totalCaseCount} CDC case documents. " +
            $"Jurisdiction errors: {sourceErrorCount}. CDC case database bulk errors: {bulkWriteErrorCount}. " +
            $"Starting CDC de-identified case database/report database rebuild.");
        return ("Finished", $"Wrote {totalCaseCount} CDC case documents.");
    }

    private static bool IsTransientBulkWriteException(Exception ex)
    {
        if (ex == null)
        {
            return false;
        }

        if (ex is HttpRequestException || ex is TaskCanceledException || ex is TimeoutException)
        {
            return true;
        }

        return IsTransientBulkWriteException(ex.InnerException);
    }

    private async Task<(int success_count, int error_count)> BulkSavePopulateCdcDocumentsWithThrottle(
        IEnumerable<string> documentJsonList,
        mmria.common.couchdb.DBConfigurationDetail cdcConnection,
        PopulateCdcPhaseThrottleSettings settings)
    {
        var document_list = documentJsonList?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList() ?? new List<string>();

        if (document_list.Count == 0)
        {
            return (0, 0);
        }

        int effective_chunk_size =
            settings?.BulkDocChunkSize > 0
            ? Math.Max(1, settings.BulkDocChunkSize)
            : document_list.Count;
        int retry_count = Math.Max(0, settings?.BulkWriteRetryCount ?? 0);
        int retry_delay_ms = Math.Max(0, settings?.BulkWriteRetryDelayMs ?? 0);
        int success_count = 0;
        int error_count = 0;

        for (int offset = 0; offset < document_list.Count; offset += effective_chunk_size)
        {
            var chunk = document_list
                .Skip(offset)
                .Take(effective_chunk_size)
                .ToList();

            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    var results = await BulkSavePopulateCdcDocuments(chunk, cdcConnection);
                    int chunk_success_count = results.Count(item => item?.ok == true);
                    success_count += chunk_success_count;
                    error_count += Math.Max(0, chunk.Count - chunk_success_count);
                    break;
                }
                catch (Exception ex)
                {
                    if (!IsTransientBulkWriteException(ex) || attempt >= retry_count)
                    {
                        throw;
                    }

                    int delay_ms = retry_delay_ms * (attempt + 1);
                    Console.WriteLine(
                        $"[PopulateCDC] Transient mmrds bulk write failure for '{cdcConnection?.url}'. " +
                        $"Retry {attempt + 1} of {retry_count} in {delay_ms} ms.\n{ex.Message}");

                    if (delay_ms > 0)
                    {
                        await Task.Delay(delay_ms);
                    }
                }
            }
        }

        return (success_count, error_count);
    }

    private static void ClearPopulateCdcLockFields(IDictionary<string, object> caseDoc)
    {
        if (caseDoc == null)
        {
            return;
        }

        caseDoc.Remove("date_last_checked_out");
        caseDoc.Remove("last_checked_out_by");
        caseDoc.Remove("checked_out_by_tab_id");
        caseDoc.Remove("is_offline");
        caseDoc.Remove("offline_by");
        caseDoc.Remove("offline_lock_type");
        caseDoc.Remove("offline_by_tab_id");
    }

    public async Task DeleteDatabaseIfExists(string databaseUrl, string userName, string userValue)
    {
        await _mmriaServicesDal.ExecuteDatabaseCall("DELETE", databaseUrl, null, userName, userValue);
    }

    public async Task CreateDatabase(string databaseUrl, string userName, string userValue)
    {
        await _mmriaServicesDal.ExecuteDatabaseCall("PUT", databaseUrl, null, userName, userValue);
    }

    public async Task SetDatabaseSecurity(string securityUrl, string securityJson, string userName, string userValue)
    {
        await _mmriaServicesDal.ExecuteDatabaseCall("PUT", securityUrl, securityJson, userName, userValue);
    }

    public async Task SaveDesignDocument(string designUrl, string designJson, string userName, string userValue)
    {
        await _mmriaServicesDal.ExecuteDatabaseCall("PUT", designUrl, designJson, userName, userValue);
    }

    public async Task CreateDatabaseIndex(string indexUrl, string indexJson, string userName, string userValue)
    {
        await _mmriaServicesDal.ExecuteDatabaseCall("POST", indexUrl, indexJson, userName, userValue);
    }

    public async Task<HashSet<string>> GetPopulateCdcCaseIds(mmria.common.couchdb.DBConfigurationDetail db_info)
    {
        return await _mmriaServicesDal.GetCaseIdsByDateCreated(db_info);
    }

    public async Task<System.Dynamic.ExpandoObject> GetPopulateCdcCaseDocument(mmria.common.couchdb.DBConfigurationDetail db_info, string case_id)
    {
        return await _mmriaServicesDal.GetCaseDocumentForPopulateCDC(db_info, case_id);
    }

    public async Task<List<ExpandoObject>> GetPopulateCdcCaseDocuments(mmria.common.couchdb.DBConfigurationDetail db_info, IEnumerable<string> case_ids)
    {
        return await _mmriaServicesDal.GetCaseDocumentsForPopulateCDC(db_info, case_ids);
    }

    public async Task SavePopulateCdcDocument(string documentJson, string targetUrl, string userName, string userValue)
    {
        await _mmriaServicesDal.ExecuteDatabaseCall("PUT", targetUrl, documentJson, userName, userValue);
    }

    public async Task<List<mmria.common.model.couchdb.document_put_response>> BulkSavePopulateCdcDocuments(
        IEnumerable<string> documentJsonList,
        mmria.common.couchdb.DBConfigurationDetail cdcConnection)
    {
        return await _mmriaServicesDal.BulkSavePopulateCdcDocumentsAsync(documentJsonList, cdcConnection);
    }

    public async Task<string> DeIdentifyCaseForPopulateCDC(
        string documentJson,
        string instanceName,
        mmria.common.couchdb.DBConfigurationDetail cdcConnection,
        string metadataReleaseVersionName,
        IEnumerable<string> deIdentifiedPaths = null
    )
    {
        var deIdentifier = new c_cdc_de_identifier(
            documentJson,
            instanceName,
            cdcConnection,
            metadataReleaseVersionName,
            _couchDbHttpClient,
            deIdentifiedPaths
        );

        return await deIdentifier.executeAsync();
    }

    private static IReadOnlyCollection<string> ResolvePopulateCdcDeIdentifiedPaths(
        IDictionary<string, HashSet<string>> deIdentifiedExportPathMap,
        string instanceName)
    {
        if
        (
            !string.IsNullOrWhiteSpace(instanceName) &&
            deIdentifiedExportPathMap != null &&
            deIdentifiedExportPathMap.TryGetValue(instanceName, out var instancePaths)
        )
        {
            return instancePaths;
        }

        if
        (
            deIdentifiedExportPathMap != null &&
            deIdentifiedExportPathMap.TryGetValue("global", out var globalPaths)
        )
        {
            return globalPaths;
        }

        return Array.Empty<string>();
    }

    private static string FormatPopulateCdcSourceDatabaseLabel(string instanceName)
    {
        if(string.IsNullOrWhiteSpace(instanceName))
        {
            return "jurisdiction case database";
        }

        return $"{instanceName} case database";
    }

    private async Task SetupPopulateCdcDatabases(mmria.common.couchdb.DBConfigurationDetail cdc_connection)
    {
        var current_directory = AppContext.BaseDirectory;
        if (!System.IO.Directory.Exists(System.IO.Path.Combine(current_directory, "database-scripts")))
        {
            current_directory = System.IO.Directory.GetCurrentDirectory();
        }

        var cdc_mmrds_url = $"{cdc_connection.url}/mmrds";
        var cdc_de_id_url = $"{cdc_connection.url}/de_id";
        var cdc_report_url = $"{cdc_connection.url}/report";

        Console.WriteLine($"[PopulateCDC] Resetting CDC databases at {cdc_connection.url}.");
        try
        {
            await DeleteDatabaseIfExists(cdc_mmrds_url, cdc_connection.user_name, cdc_connection.user_value);
            Console.WriteLine($"[PopulateCDC] Deleted existing database: {cdc_mmrds_url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PopulateCDC] Unable to delete {cdc_mmrds_url}. Continuing. {ex.Message}");
        }

        await CreateDatabase(cdc_mmrds_url, cdc_connection.user_name, cdc_connection.user_value);
        Console.WriteLine($"[PopulateCDC] Created database: {cdc_mmrds_url}");

        await SetDatabaseSecurity(
            $"{cdc_mmrds_url}/_security",
            "{\"admins\":{\"names\":[],\"roles\":[\"form_designer\"]},\"members\":{\"names\":[],\"roles\":[\"abstractor\",\"data_analyst\",\"timer\"]}}",
            cdc_connection.user_name,
            cdc_connection.user_value
        );

        try
        {
            var case_design_sortable = await System.IO.File.ReadAllTextAsync(System.IO.Path.Combine(current_directory, "database-scripts/case_design_sortable.json"));
            await SaveDesignDocument($"{cdc_mmrds_url}/_design/sortable", case_design_sortable, cdc_connection.user_name, cdc_connection.user_value);

            var case_store_design_auth = await System.IO.File.ReadAllTextAsync(System.IO.Path.Combine(current_directory, "database-scripts/case_store_design_auth.json"));
            await SaveDesignDocument($"{cdc_mmrds_url}/_design/auth", case_store_design_auth, cdc_connection.user_name, cdc_connection.user_value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"unable to configure mmrds database:\n{ex}");
        }

        try
        {
            await DeleteDatabaseIfExists(cdc_de_id_url, cdc_connection.user_name, cdc_connection.user_value);
            Console.WriteLine($"[PopulateCDC] Deleted existing database: {cdc_de_id_url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PopulateCDC] Unable to delete {cdc_de_id_url}. Continuing. {ex.Message}");
        }
        bool reportDatabaseExists = false;
        try
        {
            reportDatabaseExists = await mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper.ClearDatabaseDocumentsPreservingSystemDocsAsync(
                _couchDbHttpClient,
                cdc_report_url,
                cdc_connection.user_name,
                cdc_connection.user_value);

            if(reportDatabaseExists)
            {
                Console.WriteLine($"[PopulateCDC] Cleared existing report data docs while preserving design docs: {cdc_report_url}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"unable to clear report database:\n{ex}");
            throw;
        }

        try
        {
            await CreateDatabase(cdc_de_id_url, cdc_connection.user_name, cdc_connection.user_value);
            Console.WriteLine($"[PopulateCDC] Created database: {cdc_de_id_url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"unable to create de_id database:\n{ex}");
            throw;
        }

        try
        {
            var case_design_sortable = await System.IO.File.ReadAllTextAsync(System.IO.Path.Combine(current_directory, "database-scripts/case_design_sortable_de_id.json"));
            await SaveDesignDocument($"{cdc_de_id_url}/_design/sortable", case_design_sortable, cdc_connection.user_name, cdc_connection.user_value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"unable to configure de_id database:\n{ex}");
        }

        if(!reportDatabaseExists)
        {
            try
            {
                await CreateDatabase(cdc_report_url, cdc_connection.user_name, cdc_connection.user_value);
                Console.WriteLine($"[PopulateCDC] Created database: {cdc_report_url}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"unable to create report database:\n{ex}");
                throw;
            }
        }

        try
        {
            bool opioidIndexExists = reportDatabaseExists &&
                await mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper.UrlExistsAsync(
                    _couchDbHttpClient,
                    $"{cdc_report_url}/_design/opioid-report-index",
                    cdc_connection.user_name,
                    cdc_connection.user_value);

            if(!opioidIndexExists)
            {
                var reportOpioidIndex = new
                {
                    index = new
                    {
                        partial_filter_selector = new
                        {
                            _id = new Dictionary<string, string>() { { "$regex", "^opioid" } }
                        },
                        fields = new List<string>() { "_id" }
                    },
                    ddoc = "opioid-report-index",
                    type = "json"
                };

                string index_json = Newtonsoft.Json.JsonConvert.SerializeObject(reportOpioidIndex);
                await CreateDatabaseIndex($"{cdc_report_url}/_index", index_json, cdc_connection.user_name, cdc_connection.user_value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"unable to configure report database:\n{ex}");
        }
    }

    public async Task<mmria.common.metadata.Populate_CDC_Instance> GetPopulateCDCInstanceAsync(
        mmria.common.couchdb.DBConfigurationDetail db_config,
        string service_url,
        string vital_service_key)
    {
        var result = await _mmriaServicesDal.GetPopulateCDCInstanceDocumentAsync(db_config);

        try
        {
            var service_response = await _mmriaServicesDal.GetPopulateCDCInstanceFromServiceAsync(service_url, vital_service_key);

            if
            (
                service_response != null &&
                !string.IsNullOrWhiteSpace(service_response.transfer_result)
            )
            {
                result.transfer_result = service_response.transfer_result;
                result.transfer_status_number = service_response.transfer_status_number;
                result.date_submitted = service_response.date_submitted;
                result.date_completed = service_response.date_completed;
                result.duration_in_hours = service_response.duration_in_hours;
                result.duration_in_minutes = service_response.duration_in_minutes;
                result.error_message = service_response.error_message;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    public async Task<mmria.common.model.couchdb.document_put_response> SavePopulateCDCInstanceDocumentAsync(
        string document_content,
        mmria.common.couchdb.DBConfigurationDetail db_config)
    {
        return await _mmriaServicesDal.SavePopulateCDCInstanceDocumentAsync(document_content, db_config);
    }

    public async Task<mmria.common.metadata.Populate_CDC_Instance> PutPopulateCDCInstanceToServiceAsync(
        mmria.common.metadata.Populate_CDC_Instance request_message,
        string service_url,
        string vital_service_key)
    {
        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(request_message, settings);

        return await _mmriaServicesDal.PutPopulateCDCInstanceToServiceAsync(service_url, object_string, vital_service_key);
    }

    private async Task<TenantDatabaseCountEntryResponse> BuildTenantDatabaseCountEntryAsync(
        string entryName,
        mmria.common.couchdb.DBConfigurationDetail dbInfo,
        int perDatabaseTimeoutSeconds)
    {
        var entry = new TenantDatabaseCountEntryResponse
        {
            entry_name = entryName?.Trim()
        };

        if (dbInfo == null ||
            string.IsNullOrWhiteSpace(dbInfo.url) ||
            string.IsNullOrWhiteSpace(dbInfo.user_name) ||
            string.IsNullOrWhiteSpace(dbInfo.user_value))
        {
            const string invalidConfigurationMessage = "Missing database URL or credentials in configuration detail_list.";
            entry.mmrds_error = invalidConfigurationMessage;
            entry.de_id_error = invalidConfigurationMessage;
            entry.report_error = invalidConfigurationMessage;
            entry.status = "error";
            return entry;
        }

        var mmrdsTask = TryGetDatabaseCountAsync(dbInfo.Get_Prefix_DB_Url("mmrds"), dbInfo, perDatabaseTimeoutSeconds);
        var deIdTask = TryGetDatabaseCountAsync(dbInfo.Get_Prefix_DB_Url("de_id"), dbInfo, perDatabaseTimeoutSeconds);
        var reportTask = TryGetDatabaseCountAsync(dbInfo.Get_Prefix_DB_Url("report"), dbInfo, perDatabaseTimeoutSeconds);
        var mmrdsDesignDocCountTask = TryGetDesignDocumentCountAsync(dbInfo.Get_Prefix_DB_Url("mmrds"), dbInfo, "MMRDS", perDatabaseTimeoutSeconds);
        var deIdDesignDocCountTask = TryGetDesignDocumentCountAsync(dbInfo.Get_Prefix_DB_Url("de_id"), dbInfo, "De-ID", perDatabaseTimeoutSeconds);
        var reportDesignDocCountTask = TryGetDesignDocumentCountAsync(dbInfo.Get_Prefix_DB_Url("report"), dbInfo, "Report", perDatabaseTimeoutSeconds);

        await Task.WhenAll(mmrdsTask, deIdTask, reportTask, mmrdsDesignDocCountTask, deIdDesignDocCountTask, reportDesignDocCountTask);

        var mmrdsResult = await mmrdsTask;
        var deIdResult = await deIdTask;
        var reportResult = await reportTask;
        var mmrdsDesignDocCountResult = await mmrdsDesignDocCountTask;
        var deIdDesignDocCountResult = await deIdDesignDocCountTask;
        var reportDesignDocCountResult = await reportDesignDocCountTask;

        entry.mmrds_doc_count = mmrdsResult.doc_count;
        entry.de_id_doc_count = deIdResult.doc_count;
        entry.report_doc_count = reportResult.doc_count;
        entry.mmrds_error = CombineMessages(mmrdsResult.error, mmrdsDesignDocCountResult.error);
        entry.de_id_error = CombineMessages(deIdResult.error, deIdDesignDocCountResult.error);
        entry.report_error = CombineMessages(reportResult.error, reportDesignDocCountResult.error);

        if (entry.mmrds_doc_count.HasValue && mmrdsDesignDocCountResult.doc_count.HasValue)
        {
            entry.mmrds_comparable_doc_count = entry.mmrds_doc_count.Value - mmrdsDesignDocCountResult.doc_count.Value;
        }

        if (entry.de_id_doc_count.HasValue && deIdDesignDocCountResult.doc_count.HasValue)
        {
            entry.de_id_comparable_doc_count = entry.de_id_doc_count.Value - deIdDesignDocCountResult.doc_count.Value;
        }

        if (entry.report_doc_count.HasValue && reportDesignDocCountResult.doc_count.HasValue)
        {
            entry.report_comparable_doc_count = entry.report_doc_count.Value - reportDesignDocCountResult.doc_count.Value;
        }

        if (entry.mmrds_comparable_doc_count.HasValue && entry.de_id_comparable_doc_count.HasValue)
        {
            entry.de_id_delta_from_mmrds = entry.de_id_comparable_doc_count.Value - entry.mmrds_comparable_doc_count.Value;
        }

        if (entry.mmrds_comparable_doc_count.HasValue &&
            entry.report_comparable_doc_count.HasValue &&
            entry.mmrds_comparable_doc_count.Value > 0)
        {
            entry.report_to_mmrds_ratio = decimal.Round(
                (decimal)entry.report_comparable_doc_count.Value / entry.mmrds_comparable_doc_count.Value,
                2,
                MidpointRounding.AwayFromZero);
        }

        int errorCount = new[] { entry.mmrds_error, entry.de_id_error, entry.report_error }
            .Count(item => !string.IsNullOrWhiteSpace(item));

        entry.status = errorCount switch
        {
            0 => "ok",
            3 => "error",
            _ => "partial_error"
        };

        return entry;
    }

    private async Task<(int? doc_count, string error)> TryGetDesignDocumentCountAsync(
        string databaseUrl,
        mmria.common.couchdb.DBConfigurationDetail dbInfo,
        string databaseLabel,
        int timeoutSeconds)
    {
        try
        {
            int designDocCount = await _mmriaServicesDal.GetDesignDocumentCountAsync(
                databaseUrl,
                dbInfo.user_name,
                dbInfo.user_value,
                timeoutSeconds);

            return (designDocCount, null);
        }
        catch (TaskCanceledException)
        {
            return (null, $"{databaseLabel} comparable count timed out after {timeoutSeconds} seconds.");
        }
        catch (HttpRequestException ex)
        {
            return (null, $"{databaseLabel} comparable count unavailable: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"{databaseLabel} comparable count unavailable: {ex.Message}");
        }
    }

    private async Task<(int? doc_count, string error)> TryGetDatabaseCountAsync(
        string databaseUrl,
        mmria.common.couchdb.DBConfigurationDetail dbInfo,
        int timeoutSeconds)
    {
        try
        {
            var metadata = await _mmriaServicesDal.GetDatabaseMetadataAsync(
                databaseUrl,
                dbInfo.user_name,
                dbInfo.user_value,
                timeoutSeconds);

            if (metadata == null)
            {
                return (null, "No response returned from CouchDB.");
            }

            if (metadata["doc_count"] != null)
            {
                return (metadata.Value<int?>("doc_count"), null);
            }

            string error = metadata.Value<string>("error");
            string reason = metadata.Value<string>("reason");
            string message = string.Join(
                " ",
                new[] { error, reason }
                    .Where(item => !string.IsNullOrWhiteSpace(item)));

            return (null, string.IsNullOrWhiteSpace(message) ? "Unexpected CouchDB response." : message);
        }
        catch (TaskCanceledException)
        {
            return (null, $"Timed out after {timeoutSeconds} seconds.");
        }
        catch (HttpRequestException ex)
        {
            return (null, ex.Message);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    private static string CombineMessages(params string[] values)
    {
        var messages = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return messages.Count == 0
            ? null
            : string.Join(" ", messages);
    }

    private static Uri BuildValidatedServicesBaseUri(string vitalsUrl)
    {
        if (string.IsNullOrWhiteSpace(vitalsUrl))
        {
            throw new InvalidOperationException("The current tenant is missing vitals_url configuration.");
        }

        string servicesBaseUrl = vitalsUrl.Replace("/api/Message/IJESet", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (string.Equals(servicesBaseUrl, vitalsUrl, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The current tenant vitals_url does not contain the expected Message/IJESet path.");
        }

        if (!Uri.TryCreate(servicesBaseUrl, UriKind.Absolute, out var servicesUri))
        {
            throw new InvalidOperationException("The derived services URL is not a valid absolute URI.");
        }

        if (servicesUri.Scheme != Uri.UriSchemeHttp && servicesUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("The derived services URL must use HTTP or HTTPS.");
        }

        if (!string.IsNullOrWhiteSpace(servicesUri.UserInfo) || !string.IsNullOrWhiteSpace(servicesUri.Fragment))
        {
            throw new InvalidOperationException("The derived services URL must not contain user info or fragments.");
        }

        return new UriBuilder(servicesUri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            Path = servicesUri.AbsolutePath.TrimEnd('/') + "/"
        }.Uri;
    }

    private static (string configId, mmria.common.couchdb.DBConfigurationDetail dbConfig) ResolveCdcConfigurationAccess(
        mmria.common.couchdb.ConfigurationSet runtimeConfigSet)
    {
        if (runtimeConfigSet?.detail_list == null || runtimeConfigSet.detail_list.Count == 0)
        {
            throw new InvalidOperationException("Runtime ConfigurationSet is missing detail_list entries for CDC configuration access.");
        }

        string cdcConfigId = null;
        mmria.common.couchdb.DBConfigurationDetail cdcConnection = null;
        if (runtimeConfigSet.detail_list.TryGetValue("cdc", out var cdcDbConfig))
        {
            cdcConfigId = "cdc";
            cdcConnection = cdcDbConfig;
        }
        else if (runtimeConfigSet.detail_list.TryGetValue("cdcqa", out var cdcQaDbConfig))
        {
            cdcConfigId = "cdcqa";
            cdcConnection = cdcQaDbConfig;
        }

        if (cdcConnection == null)
        {
            throw new InvalidOperationException("Runtime ConfigurationSet detail_list is missing cdc/cdcqa needed to reach the CDC configuration database.");
        }

        if (string.IsNullOrWhiteSpace(cdcConnection.url) ||
            string.IsNullOrWhiteSpace(cdcConnection.user_name) ||
            string.IsNullOrWhiteSpace(cdcConnection.user_value))
        {
            throw new InvalidOperationException("Resolved CDC configuration database connection is missing URL or credentials.");
        }

        return (cdcConfigId, cdcConnection);
    }
}
