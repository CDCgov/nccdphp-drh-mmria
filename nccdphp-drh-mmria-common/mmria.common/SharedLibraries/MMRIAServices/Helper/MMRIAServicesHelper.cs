using System;
using System.Collections.Generic;

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
}