using mmria.common.SharedLibraries.MMRIAServices.DAL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mmria.common.SharedLibraries.MMRIAServices.Manager;

public sealed class MMRIAServicesManager
{
    private readonly MMRIAServicesDAL _mmriaServicesDal;
    private readonly System.Net.Http.HttpClient _externalHttpClient;

    public MMRIAServicesManager(MMRIAServicesDAL mmriaServicesDal)
    {
        _mmriaServicesDal = mmriaServicesDal;
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
        Func<string, string, mmria.common.couchdb.DBConfigurationDetail, string, Task<string>> deIdentifyAsync
    )
    {
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

        await SetupPopulateCdcDatabases(cdc_connection);

        for (var i = 0; i < message.state_list.Count; i++)
        {
            if (message.state_list[i].is_included != true)
            {
                continue;
            }

            var instance_name = message.state_list[i].prefix;
            if (!db_config_set.detail_list.ContainsKey(instance_name))
            {
                continue;
            }

            try
            {
                var db_info = db_config_set.detail_list[instance_name];
                var caseIds = await GetPopulateCdcCaseIds(db_info);

                foreach (var case_id in caseIds)
                {
                    var case_row = await GetPopulateCdcCaseDocument(db_info, case_id);
                    var case_doc = case_row as IDictionary<string, object>;

                    if
                    (
                        case_doc == null ||
                        !case_doc.ContainsKey("_id") ||
                        case_doc["_id"] == null ||
                        case_doc["_id"].ToString().StartsWith("_design", StringComparison.InvariantCultureIgnoreCase)
                    )
                    {
                        continue;
                    }

                    string _id = case_doc["_id"].ToString();
                    var target_url = $"{cdc_connection.url}/mmrds/{_id}";

                    var document_json = Newtonsoft.Json.JsonConvert.SerializeObject(case_doc);
                    var de_identified_json = await deIdentifyAsync(document_json, instance_name, cdc_connection, metadata_release_version_name);

                    var de_identified_case = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(de_identified_json);
                    case_doc["_rev"] = null;

                    var de_identified_dictionary = de_identified_case as IDictionary<string, object>;
                    if (de_identified_dictionary == null)
                    {
                        continue;
                    }

                    var save_json = Newtonsoft.Json.JsonConvert.SerializeObject(de_identified_dictionary);
                    await SavePopulateCdcDocument(save_json, target_url, cdc_connection.user_name, cdc_connection.user_value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Problem pulling instance:{instance_name}");
                Console.WriteLine(ex);
            }
        }

        return ("Finished", "");
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

    public async Task SavePopulateCdcDocument(string documentJson, string targetUrl, string userName, string userValue)
    {
        await _mmriaServicesDal.ExecuteDatabaseCall("PUT", targetUrl, documentJson, userName, userValue);
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

        try { await DeleteDatabaseIfExists(cdc_mmrds_url, cdc_connection.user_name, cdc_connection.user_value); } catch { }

        await CreateDatabase(cdc_mmrds_url, cdc_connection.user_name, cdc_connection.user_value);

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

        try { await DeleteDatabaseIfExists(cdc_de_id_url, cdc_connection.user_name, cdc_connection.user_value); } catch { }
        try { await DeleteDatabaseIfExists(cdc_report_url, cdc_connection.user_name, cdc_connection.user_value); } catch { }

        try { await CreateDatabase(cdc_de_id_url, cdc_connection.user_name, cdc_connection.user_value); } catch { }

        try
        {
            var case_design_sortable = await System.IO.File.ReadAllTextAsync(System.IO.Path.Combine(current_directory, "database-scripts/case_design_sortable.json"));
            await SaveDesignDocument($"{cdc_de_id_url}/_design/sortable", case_design_sortable, cdc_connection.user_name, cdc_connection.user_value);
        }
        catch { }

        try { await CreateDatabase(cdc_report_url, cdc_connection.user_name, cdc_connection.user_value); } catch { }

        try
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
        catch { }
    }
}