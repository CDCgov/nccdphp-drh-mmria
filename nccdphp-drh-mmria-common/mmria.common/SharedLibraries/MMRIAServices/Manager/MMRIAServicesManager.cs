using mmria.common.SharedLibraries.MMRIAServices.DAL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mmria.common.SharedLibraries.MMRIAServices.Manager;

public sealed class MMRIAServicesManager
{
    private readonly MMRIAServicesDAL _mmriaServicesDal;

    public MMRIAServicesManager(MMRIAServicesDAL mmriaServicesDal)
    {
        _mmriaServicesDal = mmriaServicesDal;
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