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
}