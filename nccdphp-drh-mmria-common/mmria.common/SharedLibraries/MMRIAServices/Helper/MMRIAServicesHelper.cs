using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace mmria.common.SharedLibraries.MMRIAServices.Helper;

public static class MMRIAServicesHelper
{
    public static bool validate_length(IList<string> p_array, int p_max_length)
    {
        var result = true;

        if(p_array != null)
            for(var i = 0; i < p_array.Count; i++)
            {
                var item = p_array[i];
                if(item.Length > 0 && item.Length != p_max_length)
                {
                    result = false;
                    break;
                }
            }

        return result;
    }

    public static IList<string> validate_AssociatedNAT(IList<string> p_array, HashSet<string> g_cdc_identifier_set)
    {
        var result = new List<string>();

        int mom_ssn_start = 2000-1;

        for (var i = 0; i < p_array.Count; i++)
        {
            var item = p_array[i];
            if (item.Length > mom_ssn_start + 9)
            {
                // Don't store SSN in a variable - use inline comparison
                if (!g_cdc_identifier_set.Contains(item.Substring(mom_ssn_start, 9).Trim()))
                {
                    result.Add($"Missing identifier in NAT file at line: {i+1}");
                }
            }
        }

        return result;
    }

    public static IList<string> validate_AssociatedFET(IList<string> p_array, HashSet<string> g_cdc_identifier_set)
    {
        var result = new List<string>();

        int mom_ssn_start = 4039-1;

        for (var i = 0; i < p_array.Count; i++)
        {
            var item = p_array[i];
            if (item.Length > mom_ssn_start + 9)
            {
                // Don't store SSN in a variable - use inline comparison
                if (!g_cdc_identifier_set.Contains(item.Substring(mom_ssn_start, 9).Trim()))
                {
                    result.Add($"Missing identifier in FET file at line: {i+1}");
                }
            }
        }

        return result;
    }

    public static mmria.common.ije.BatchItem ConvertLineToBatchItem
    (
            string LineItem,
            DateTime ImportDate,
            string ImportFileName,
            string ReportingState,
            HashSet<string> ExistingRecordIds
    )
    {
        /*
        CDCUniqueID
            ImportDate
            ImportFileName
            ReportingState
            StateOfDeathRecord
            DateOfDeath
            DateOfBirth
            LastName
            FirstName
            MMRIARecordID
            StatusDetail
            */

        var x = mor_get_header(LineItem);

        string record_id = null;

        do
        {
            record_id = $"{ReportingState.ToUpper()}-{x["DOD_YR"]}-{GenerateRandomFourDigits().ToString()}";
        }
        while (ExistingRecordIds.Contains(record_id));
        ExistingRecordIds.Add(record_id);

        var result = new mmria.common.ije.BatchItem()
        {
            Status = mmria.common.ije.BatchItem.StatusEnum.InProcess,
            CDCUniqueID = x["SSN"]?.Trim(),
            mmria_record_id = record_id,
            ImportDate = ImportDate,
            ImportFileName = ImportFileName,
            ReportingState = ReportingState,

            StateOfDeathRecord = x["DSTATE"],
            DateOfDeath = $"{x["DOD_YR"]}-{x["DOD_MO"]}-{x["DOD_DY"]}",
            DateOfBirth = $"{x["DOB_YR"]}-{x["DOB_MO"]}-{x["DOB_DY"]}",
            LastName = x["LNAME"],
            FirstName = x["GNAME"]//,
            //MMRIARecordID = x[""],
            //StatusDetail = x[""]
        };

        return result;
    }

    public static List<mmria.common.ije.BatchItem> ConvertBatchItemDictionaryToList(Dictionary<string,(string, mmria.common.ije.BatchItem)> p_val)
    {
        List<mmria.common.ije.BatchItem> result = new();

        foreach(var kvp in p_val)
        {
            result.Add(kvp.Value.Item2);
        }

        return result;
    }

    public static string get_state_from_file_name(string p_val)
    {
        var remove_extension = p_val.Split(".");
        var split_on_underscore = remove_extension[0].Split("_");

        return split_on_underscore[split_on_underscore.Length -1];
    }

    public static Dictionary<string,string> mor_get_header(string row)
    {
            var result = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            /*
DState 5 2
DOD_YR 1 4,
DOD_MO 237 2,
DOD_DY 239 2
DOB_YR 205 4,
DOB_MO 209 2,
DOD_DY 239 2
LNAME 78 50
GNAME 27 50
*/
result.Add("DState",row.Substring(5-1, 2));
result.Add("DOD_YR",row.Substring(1-1, 4));
result.Add("DOD_MO",row.Substring(237-1, 2));
result.Add("DOD_DY",row.Substring(239-1, 2));
result.Add("DOB_YR",row.Substring(205-1, 4));
result.Add("DOB_MO",row.Substring(209-1, 2));
result.Add("DOB_DY",row.Substring(211-1, 2));
result.Add("LNAME",row.Substring(78-1, 50));
result.Add("GNAME",row.Substring(27-1, 50));
result.Add("SSN",row.Substring(191-1, 9)?.Trim());

        return result;

        /*
        2 home_record/state of death - DState
3 home_recode/date_of_death - DOD_YR, DOD_MO, DOD_DY
4 death_certificate/date_of_birth - DOB_YR, DOB_MO, DOD_DY
5 home_record/last_name - LNAME
6 home_record/first_name - GNAME*/
    }

    public static List<string> GetAssociatedNat(string[] p_nat_list, string p_cdc_unique_id)
    {
        var result = new List<string>();
        int mom_ssn_start = 2000-1;
        if (p_nat_list != null)
            foreach (var item in p_nat_list)
            {
                if (item.Length > mom_ssn_start + 9)
                {
                    var mom_ssn = item.Substring(mom_ssn_start, 9)?.Trim();
                    if (mom_ssn == p_cdc_unique_id)
                    {
                        result.Add(item);
                    }
                }
            }

        return result;
    }

    public static List<string> GetAssociatedFet(string[] p_fet_list, string p_cdc_unique_id)
    {
        var result = new List<string>();
        int mom_ssn_start = 4039-1;
        if(p_fet_list != null)
            foreach(var item in p_fet_list)
            {
                if(item.Length > mom_ssn_start + 9)
                {
                    var mom_ssn = item.Substring(mom_ssn_start, 9)?.Trim();
                    if(mom_ssn == p_cdc_unique_id)
                    {
                        result.Add(item);
                    }
                }
            }

        return result;
    }

    public static int GenerateRandomFourDigits()
    {
        int _min = 1000;
        int _max = 9999;
        return System.Security.Cryptography.RandomNumberGenerator.GetInt32(_min, _max + 1);
    }

    public static (string weeks, string days) CALCULATE_GESTATIONAL_AGE_AT_BIRTH_ON_BC
    (
        migrate.C_Get_Set_Value.get_value_result p_event_year_get_result,
        migrate.C_Get_Set_Value.get_value_result  p_event_month_get_result,
        migrate.C_Get_Set_Value.get_value_result  p_event_day_get_result,
        migrate.C_Get_Set_Value.get_value_result  p_lmp_year_get_result,
        migrate.C_Get_Set_Value.get_value_result  p_lmp_month_get_result,
        migrate.C_Get_Set_Value.get_value_result  p_lmp_day_get_result
    ) 
    {
        var result = ("","");


        bool is_valid_date(int year, int month, int day)
        {

            if
            (
                year < 1920 ||
                month == -1 ||
                day == -1 ||
                year > 3000 ||
                month > 12 ||
                day > 31
            )
            {
                return false;
            }




            var months31 = new HashSet<int>()
            {
                    1,
                    3,
                    5,
                    7,
                    8,
                    10,
                    12
            };
            var months30 = new HashSet<int>()
            {
                    4,
                    6,
                    9,
                    11
            };
            var months28 = new HashSet<int>(){2};
            var isLeap = year % 4 == 0 && year % 100 != 0 || year % 400 == 0;
            var valid = 
                months31.Contains(month) && day <= 31 || 
                months30.Contains(month)  && day <= 30 || 
                months28.Contains(month) && day <= 28 || 
                months28.Contains(month) && day <= 29 && isLeap;
            return valid;
        }
        int convert_from_dynamic_to_int(dynamic p_value)
        {
            int result = -1;
            if(p_value != null)
            {
                int.TryParse(p_value.ToString(), out result);
            }
            return result;
        }
        int calc_days(DateTime p_start_date, DateTime p_end_date) 
        {
            int days = (int) (p_end_date - p_start_date).TotalDays;
            return days;
        }

        (int weeks, int days) calc_ga_lmp(DateTime p_start_date, DateTime p_end_date) 
        {
            var weeks = calc_days(p_start_date, p_end_date) / 7;
            var days = calc_days(p_start_date, p_end_date) % 7;
            return (weeks, days);
        }

        object p_event_year_dynamic;
        object p_event_month_dynamic;
        object p_event_day_dynamic;
        object p_lmp_year_dynamic;
        object p_lmp_month_dynamic;
        object p_lmp_day_dynamic;


        if
        (
            p_event_year_get_result.is_error ||
            p_event_month_get_result.is_error ||
            p_event_day_get_result.is_error ||
            p_lmp_year_get_result.is_error ||
            p_lmp_month_get_result.is_error ||
            p_lmp_day_get_result.is_error
        )
        {
            return result;
        }
        else
        {
            p_event_year_dynamic = p_event_year_get_result.result;
            p_event_month_dynamic = p_event_month_get_result.result;
            p_event_day_dynamic = p_event_day_get_result.result;
            p_lmp_year_dynamic = p_lmp_year_get_result.result;
            p_lmp_month_dynamic = p_lmp_month_get_result.result;
            p_lmp_day_dynamic = p_lmp_day_get_result.result;
        }


        int event_year = convert_from_dynamic_to_int(p_event_year_dynamic);
        int event_month = convert_from_dynamic_to_int(p_event_month_dynamic);
        int event_day = convert_from_dynamic_to_int(p_event_day_dynamic);
        int lmp_year = convert_from_dynamic_to_int(p_lmp_year_dynamic);
        int lmp_month = convert_from_dynamic_to_int(p_lmp_month_dynamic);
        int lmp_day = convert_from_dynamic_to_int(p_lmp_day_dynamic);
        
        if 
        (
            is_valid_date(event_year, event_month, event_day) && 
            is_valid_date(lmp_year, lmp_month, lmp_day)
        ) 
        {
            try
            {
                var lmp_date = new DateTime(lmp_year, lmp_month, lmp_day);
                var event_date = new DateTime(event_year, event_month, event_day);

                var int_result = calc_ga_lmp(lmp_date, event_date);
                if(int_result.weeks > -1 && int_result.days > -1)
                {
                    result = (int_result.weeks.ToString(), int_result.days.ToString());
                }
            }
            catch(Exception)
            {

            }

        }

        return result;
    }

    public static async Task<mmria.common.niosh.NioshResult> get_niosh_codes(string p_occupation, string p_industry, mmria.common.getset.CouchDbHttpClient couchDbHttpClient)
    {
        var result = new mmria.common.niosh.NioshResult();
        var builder = new StringBuilder();
        builder.Append("https://wwwn.cdc.gov/nioccs/IOCode?n=3");
        var has_occupation = false;
        var has_industry = false;

        if(!string.IsNullOrWhiteSpace(p_occupation))
        {
            has_occupation = true;
            builder.Append($"&o={p_occupation}");
        }

        if(!string.IsNullOrWhiteSpace(p_industry))
        {
            has_industry = true;
            builder.Append($"&i={p_industry}");
        }

        if(has_occupation || has_industry)
        {
            var niosh_url = builder.ToString();

            try
            {
                string responseFromServer = await couchDbHttpClient.ExecuteAsync("GET", niosh_url, null, null, null);

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.niosh.NioshResult>(responseFromServer);
            }
            catch
            {
                result.is_error = true;
            }
            
        }

        return result;

    }

    public static string get_year_and_quarter(object p_value)
    {
        var result = string.Empty;
        
        if(p_value != null && !string.IsNullOrWhiteSpace(p_value.ToString()))
        try
        {
    
            if(p_value is DateTime)
            {
                var date_time = (DateTime) p_value;
                result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
            }
            else
            {
                var date_string = p_value.ToString();
                if(date_string.IndexOf("-") > -1)
                {
                    var int_array = date_string.Split("-");
                    if(int_array.Length == 3)
                    {
                        DateTime date_time = new DateTime(int.Parse(int_array[0]), int.Parse(int_array[1]), int.Parse(int_array[2]));
                        result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                    }
                    else
                    {
                        DateTime date_time = DateTime.ParseExact
                        (
                            date_string,
                            "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture
                        );
                        result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                    }
                }
                else if(date_string.IndexOf("/") > -1)
                {
                    DateTime date_time = DateTime.ParseExact
                    (
                        date_string,
                        "MM/dd/yyyy", 
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                }
                else
                {
                    DateTime date_time = DateTime.ParseExact
                    (
                        date_string,
                        "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                    result = $"Q{System.Math.Floor(((date_time.Month -1) / 3D) + 1D)}-{date_time.Year}";
                }
            }
        
        }
        catch
        {
        }

        return result;
    }

    public static async Task<(string, mmria.common.cvs.tract_county_result)> GetCVSData
    (
        string c_geoid,
        string t_geoid,
        string year,
        mmria.common.couchdb.ConfigurationSet ConfigDB,
        HttpClient externalHttpClient
    ) 
    { 

        mmria.common.cvs.tract_county_result result = null;
        var response_string = string.Empty;

        var base_url = ConfigDB.name_value["cvs_api_url"];

        try
        {
            
            var get_all_data_body = new mmria.common.cvs.get_all_data_post_body()
            {
                id = ConfigDB.name_value["cvs_api_id"],
                secret = ConfigDB.name_value["cvs_api_key"],
                payload = new()
                {
                    
                    c_geoid = c_geoid,
                    t_geoid = t_geoid,
                    year = year
                }
            };

            var body_text =  System.Text.Json.JsonSerializer.Serialize(get_all_data_body);

            var content = new System.Net.Http.StringContent(body_text, System.Text.Encoding.UTF8, "application/json");
            var response = await externalHttpClient.PostAsync(base_url, content);
            response_string = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine(response_string);

            result = System.Text.Json.JsonSerializer.Deserialize<mmria.common.cvs.tract_county_result>(response_string);
        
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController  POST\n{ex}");

            return ($"Status: {ex.Status} {ex.Message} {response_string}", null);
        }

        return ("success", result);
    }

    public static async Task<List<int>> CVS_Get_Valid_Years(mmria.common.couchdb.ConfigurationSet ConfigDB, HttpClient externalHttpClient) 
    { 
        var result = new List<int>()
        {
            2010,
            2011,
            2012,
            2013,
            2014,
            2015,
            2016,
            2017,
            2018,
            2019,
            2020
        };

        var base_url = ConfigDB.name_value["cvs_api_url"];

        try
        {
            var get_year_body = new mmria.common.cvs.get_year_post_body()
            {
                id = ConfigDB.name_value["cvs_api_id"],
                secret = ConfigDB.name_value["cvs_api_key"],
                payload = new()
            };

            var body_text =  System.Text.Json.JsonSerializer.Serialize(get_year_body);
            var content = new System.Net.Http.StringContent(body_text, System.Text.Encoding.UTF8, "application/json");
            var response = await externalHttpClient.PostAsync(base_url, content);
            string get_year_response = await response.Content.ReadAsStringAsync();

            System.Console.WriteLine(get_year_response);

    
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController Get Year POST\n{ex}");
        }

        return result;
    }	

    public static bool is_result_quality_in_need_of_checking(mmria.common.cvs.tract_county_result val)
    {

        var over_all_result = false;
        var tract_result = false;
        var county_result = false;

        const float tract_total = 11F;
        const float county_total = 26F;

        float tract_zero_count = 0F;
        float county_zero_count = 0F;

        const float _30_percent_correct = .3F;

        if
        (
            val.tract.pctMOVE == 0  &&
            val.tract.pctNOIns_Fem == 0 &&
            val.county.pctNoVehicle == 0 &&
            val.tract.pctNoVehicle == 0 &&
            val.tract.pctOWNER_OCC == 0
        )
        {
            over_all_result = true;
        }


        if(val.tract.pctNOIns_Fem == 0) tract_zero_count += 1;
        if(val.tract.MEDHHINC == 0) tract_zero_count += 1;
        if(val.tract.pctNoVehicle == 0) tract_zero_count += 1;
        if(val.tract.pctMOVE == 0) tract_zero_count += 1;
        if(val.tract.pctSPHH == 0) tract_zero_count += 1;
        if(val.tract.pctOVERCROWDHH == 0) tract_zero_count += 1;
        if(val.tract.pctOWNER_OCC == 0) tract_zero_count += 1;
        if(val.tract.pct_less_well == 0) tract_zero_count += 1;
        if(val.tract.NDI_raw == 0) tract_zero_count += 1;
        if(val.tract.pctPOV == 0) tract_zero_count += 1;
        if(val.tract.ICE_INCOME_all == 0) tract_zero_count += 1;



        if(val.county.MDrate == 0) county_zero_count += 1;
        if(val.county.pctNOIns_Fem == 0) county_zero_count += 1;
        if(val.county.pctNoVehicle == 0) county_zero_count += 1;
        if(val.county.pctMOVE == 0) county_zero_count += 1;
        if(val.county.pctSPHH == 0) county_zero_count += 1;
        if(val.county.pctOVERCROWDHH == 0) county_zero_count += 1;
        if(val.county.pctOWNER_OCC == 0) county_zero_count += 1;
        if(val.county.pct_less_well == 0) county_zero_count += 1;
        if(val.county.NDI_raw == 0) county_zero_count += 1;
        if(val.county.pctPOV == 0) county_zero_count += 1;
        if(val.county.ICE_INCOME_all == 0) county_zero_count += 1;
        if(val.county.MEDHHINC == 0) county_zero_count += 1;
        if(val.county.pctOBESE == 0) county_zero_count += 1;
        if(val.county.FI == 0) county_zero_count += 1;
        if(val.county.CNMrate == 0) county_zero_count += 1;
        if(val.county.OBGYNrate == 0) county_zero_count += 1;
        if(val.county.rtTEENBIRTH == 0) county_zero_count += 1;
        if(val.county.rtSTD == 0) county_zero_count += 1;
        if(val.county.rtMHPRACT == 0) county_zero_count += 1;
        if(val.county.rtDRUGODMORTALITY == 0) county_zero_count += 1;
        if(val.county.rtOPIOIDPRESCRIPT == 0) county_zero_count += 1;
        if(val.county.SocCap == 0) county_zero_count += 1;
        if(val.county.rtSocASSOC == 0) county_zero_count += 1;
        if(val.county.pctHOUSE_DISTRESS == 0) county_zero_count += 1;
        if(val.county.rtVIOLENTCR_ICPSR == 0) county_zero_count += 1;
        if(val.county.isolation == 0) county_zero_count += 1;


        if(tract_zero_count / tract_total < _30_percent_correct) tract_result = true;

        if(county_zero_count / county_total < _30_percent_correct) county_result = true;


        return over_all_result || tract_result || county_result;
    }
}