using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mmria.common.SharedLibraries.MMRIAServices.Helper;

public sealed class BatchImportInitializationResult
{
    public string[] MorSet { get; init; } = Array.Empty<string>();
    public StringBuilder StatusBuilder { get; init; } = new();
    public bool IsValidFileName { get; init; }
    public string ReportingState { get; init; } = string.Empty;
    public DateTime ImportDate { get; init; }
    public mmria.common.couchdb.DBConfigurationDetail ItemDbInfo { get; init; }
}

public static class MMRIAServicesHelper
{
    public static bool HasUsableDatabaseConfiguration(mmria.common.couchdb.DBConfigurationDetail itemDbInfo)
    {
        return
            itemDbInfo != null &&
            !string.IsNullOrWhiteSpace(itemDbInfo.url) &&
            !string.IsNullOrWhiteSpace(itemDbInfo.user_name) &&
            !string.IsNullOrWhiteSpace(itemDbInfo.user_value);
    }

    public static string ResolveDatabaseScriptPath(string scriptFileName)
    {
        if(string.IsNullOrWhiteSpace(scriptFileName))
        {
            throw new ArgumentException("scriptFileName is required.", nameof(scriptFileName));
        }

        var safeFileName = System.IO.Path.GetFileName(scriptFileName);
        var candidateDirectories = new[]
        {
            System.IO.Path.Combine(AppContext.BaseDirectory, "database-scripts"),
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "database-scripts"),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "source-code", "mmria", "mmria-server", "database-scripts")),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "..", "source-code", "mmria", "mmria-server", "database-scripts"))
        }
        .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach(var candidateDirectory in candidateDirectories)
        {
            var candidatePath = System.IO.Path.Combine(candidateDirectory, safeFileName);
            if(System.IO.File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        throw new System.IO.FileNotFoundException($"Unable to find database script '{safeFileName}'.");
    }

    public static async Task<bool> UrlExistsAsync(
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        string url,
        string userName,
        string userValue)
    {
        if(couchDbHttpClient == null)
        {
            throw new ArgumentNullException(nameof(couchDbHttpClient));
        }

        if(string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("url is required.", nameof(url));
        }

        try
        {
            await couchDbHttpClient.ExecuteAsync(
                "HEAD",
                url,
                null,
                userName,
                userValue,
                timeoutSeconds: 300,
                throwOnError: true);

            return true;
        }
        catch (HttpRequestException ex) when (IsNotFound(ex))
        {
            return false;
        }
    }

    public static async Task<bool> ClearDatabaseDocumentsPreservingSystemDocsAsync(
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        string databaseUrl,
        string userName,
        string userValue,
        int batchSize = 500)
    {
        if(couchDbHttpClient == null)
        {
            throw new ArgumentNullException(nameof(couchDbHttpClient));
        }

        if(string.IsNullOrWhiteSpace(databaseUrl))
        {
            throw new ArgumentException("databaseUrl is required.", nameof(databaseUrl));
        }

        if(batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "batchSize must be at least 1.");
        }

        if(!await UrlExistsAsync(couchDbHttpClient, databaseUrl, userName, userValue))
        {
            return false;
        }

        string nextStartKey = null;
        for(;;)
        {
            int requestedRowCount = batchSize;
            var queryParameters = new List<string>();
            if(!string.IsNullOrWhiteSpace(nextStartKey))
            {
                requestedRowCount++;
                string startKeyParameter = Uri.EscapeDataString(JsonConvert.SerializeObject(nextStartKey));
                queryParameters.Add($"startkey={startKeyParameter}");
            }

            queryParameters.Add($"limit={requestedRowCount}");

            string response = await couchDbHttpClient.ExecuteAsync(
                "GET",
                $"{databaseUrl}/_all_docs?{string.Join("&", queryParameters)}",
                null,
                userName,
                userValue,
                timeoutSeconds: 300,
                throwOnError: true);

            var payload = JObject.Parse(response);
            var rows = payload["rows"] as JArray;
            if(rows == null || rows.Count == 0)
            {
                break;
            }

            string lastRowId = null;
            var docsToDelete = new JArray();

            foreach(var row in rows.OfType<JObject>())
            {
                string id = row.Value<string>("id");
                if(string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                lastRowId = id;

                if(!string.IsNullOrWhiteSpace(nextStartKey) &&
                    string.Equals(id, nextStartKey, StringComparison.Ordinal))
                {
                    continue;
                }

                if(IsSystemDocumentId(id))
                {
                    continue;
                }

                string rev = row["value"]?["rev"]?.ToString();
                if(string.IsNullOrWhiteSpace(rev))
                {
                    continue;
                }

                docsToDelete.Add(new JObject
                {
                    ["_id"] = id,
                    ["_rev"] = rev,
                    ["_deleted"] = true
                });
            }

            if(docsToDelete.Count > 0)
            {
                string bulkDeletePayload = new JObject
                {
                    ["docs"] = docsToDelete
                }.ToString(Formatting.None);

                await couchDbHttpClient.ExecuteAsync(
                    "POST",
                    $"{databaseUrl}/_bulk_docs",
                    bulkDeletePayload,
                    userName,
                    userValue,
                    timeoutSeconds: 300,
                    throwOnError: true);
            }

            if(string.IsNullOrWhiteSpace(lastRowId) || rows.Count < requestedRowCount)
            {
                break;
            }

            nextStartKey = lastRowId;
        }

        return true;
    }

    private static bool IsSystemDocumentId(string documentId)
    {
        return
            documentId.StartsWith("_design/", StringComparison.OrdinalIgnoreCase) ||
            documentId.StartsWith("_local/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNotFound(HttpRequestException exception)
    {
        string message = exception?.Message ?? string.Empty;
        return
            message.Contains("404", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("not_found", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Object Not Found", StringComparison.OrdinalIgnoreCase);
    }

    public static BatchImportInitializationResult InitializeBatchImport(
        mmria.common.ije.NewIJESet_Message message,
        mmria.common.couchdb.ConfigurationSet db_config_set,
        int mor_max_length,
        int nat_max_length,
        int fet_max_length,
        string? vitalsImportAdditionalTenantsCsv)
    {
        Console.WriteLine($"Process_Message started");
        Console.WriteLine($"Processing Message : {message}");
        Console.WriteLine($"MOR length: {message?.mor?.Length ?? 0}, NAT length: {message?.nat?.Length ?? 0}, FET length: {message?.fet?.Length ?? 0}");

        var mor_set = message.mor.Split("\n");
        Console.WriteLine($"MOR lines: {mor_set?.Length ?? 0}");

        var status_builder = new StringBuilder();

        var is_valid_file_name = false;
        Console.WriteLine("Validating lengths");

        var mor_length_is_valid = validate_length(message?.mor?.Split("\n"), mor_max_length);
        var nat_length_is_valid = validate_length(message?.nat?.Split("\n"), nat_max_length);
        var fet_length_is_valid = validate_length(message?.fet?.Split("\n"), fet_max_length);

        Console.WriteLine("Checking file names");

        var ReportingState = get_state_from_file_name(message.mor_file_name);
        var additionalTenants = ParseCommaSeparatedValues(vitalsImportAdditionalTenantsCsv);

        if (additionalTenants.Contains(ReportingState))
        {
            string tenantAlternation = string.Join("|", additionalTenants.Select(System.Text.RegularExpressions.Regex.Escape));
            var patt = new System.Text.RegularExpressions.Regex(
                $"^[0-9]{{4}}_20[0-9]{{2}}_[0-2][0-9]_[0-3][0-9]_({tenantAlternation})\\.[mM][oO][rR]$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!patt.IsMatch(message.mor_file_name))
            {
                status_builder.AppendLine($"mor file name format incorrect. File name must be in ####_20##_Year_Month_Day_{ReportingState.ToUpperInvariant()} format. (e.g. 2026_2026_01_18_{ReportingState.ToUpperInvariant()}.MOR)");
            }
        }
        else
        {
            var patt = new System.Text.RegularExpressions.Regex("^[0-9]{4}_20[0-9]{2}_[0-2][0-9]_[0-3][0-9]_[A-Z]{2,9}.[mM][oO][rR]$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!patt.IsMatch(message.mor_file_name))
            {
                status_builder.AppendLine("mor file name format incorrect. File name must be in ####_20##_Year_Month_Day_StateCode format. (e.g. 2020_2021_01_01_KS.mor)");
            }
        }

        if (!mor_length_is_valid) status_builder.AppendLine("mor length is invalid.");
        if (!nat_length_is_valid) status_builder.AppendLine("nat length is invalid.");
        if (!fet_length_is_valid) status_builder.AppendLine("fet length is invalid.");

        var ImportDate = DateTime.Now;
        Console.WriteLine($"ReportingState: {ReportingState}");

        mmria.common.couchdb.DBConfigurationDetail item_db_info = null;
        if
        (
            db_config_set?.detail_list != null &&
            db_config_set.detail_list.TryGetValue(ReportingState, out item_db_info) &&
            HasUsableDatabaseConfiguration(item_db_info)
        )
        {
            is_valid_file_name = true;
        }
        else
        {
            if(db_config_set?.detail_list != null && db_config_set.detail_list.ContainsKey(ReportingState))
            {
                status_builder.AppendLine($"Database configuration is missing or incomplete for reporting state {ReportingState}");
            }
            else
            {
                status_builder.AppendLine($"Invalid reporting state {ReportingState}");
            }
        }

        return new BatchImportInitializationResult
        {
            MorSet = mor_set,
            StatusBuilder = status_builder,
            IsValidFileName = is_valid_file_name,
            ReportingState = ReportingState,
            ImportDate = ImportDate,
            ItemDbInfo = item_db_info
        };
    }

    private static HashSet<string> ParseCommaSeparatedValues(string? csv)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(csv))
        {
            return result;
        }

        foreach (string rawValue in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(rawValue))
            {
                result.Add(rawValue);
            }
        }

        return result;
    }

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

    //Transfer number verbatim to MMRIA field, format as MMRIA time.; if TOD = 9999 then this field should be left blank
    public static string TB_NAT_Rule(string value)
    {
        string parsedValue = "";

        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value == "9999")
                parsedValue = "";
            else
            {
                parsedValue = ConvertHHmm_To_MMRIATime(value);
            }
        }

        return parsedValue;
    }

    //Transfer number verbatim to MMRIA field, format as MMRIA time.; if TOD = 9999 then this field should be left blank
    public static string TD_FET_Rule(string value)
    {
        string parsedValue = "";

        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value == "9999")
                parsedValue = "";
            else
            {
                parsedValue = ConvertHHmm_To_MMRIATime(value);

            }
        }

        return parsedValue;
    }

    /*
        42 => 00:42:00
        1945 => 19:45:00
        1530 => 15:30:00
        815 => 08:15:00


        42 => 00:42
        1945 => 19:45
        1530 => 15:30
        815 => 08:15
    */
    //Ensure three digit times parse with 4 digits, e.g. 744 becomes 0744 and will be parsed to 7:44 AM
    public static string ConvertHHmm_To_MMRIATime(string value)
    {
        string result = value;
        try
        {
            switch (value.Length)
            {
                case 0:
                    break;
                case 1:
                    result = $"00:0{value}:00";
                    break;
                case 2:
                    result = $"00:{value}:00";
                    break;
                case 3:
                    result = $"0{value[0]}:{value[1..^0]}:00";
                    break;
                case 4:
                    result = $"{value[0..2]}:{value[2..^0]}:00";
                    break;
                default:
                    System.Console.Write($"ConvertHHmm_To_MMRIATime unable to convert {value}");
                    break;
            }
        }
        catch (Exception)
        {
        }

        return result;
    }

    //"Map XX --> 9999 (blank)
    //Map ZZ --> 9999(blank)
    //Map all other values to MMRIA field state listing"
    public static string STATEC_FET_Rule(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    //CALCULATE MOTHERS AGE AT DELIVERY ON BC
    /*
    path=birth_fetal_death_certificate_parent/demographic_of_mother/age
    event=onfocus
    */
    public static string age_delivery(string dob_YR, string dob_MO, string dob_day, string dodeliv_YR, string dodeliv_MO, string dodeliv_day)
    {
        string years = "";
        int.TryParse(dob_YR, out int start_year);
        int.TryParse(dob_MO, out int start_month);
        int.TryParse(dob_day, out int start_day);
        int.TryParse(dodeliv_YR, out int end_year);
        int.TryParse(dodeliv_MO, out int end_month);
        int.TryParse(dodeliv_day, out int end_day);

        if
        (
            DateTime.TryParse($"{start_year}/{start_month}/{start_day}", out DateTime birthDateCheck) == true &&
            DateTime.TryParse($"{end_year}/{end_month}/{end_day}", out DateTime endDateCheck) == true
        )
        {
            var start_date = new DateTime(start_year, start_month, start_day).AddMonths(-1);
            var end_date = new DateTime(end_year, end_month, end_day).AddMonths(-1);
            years = calc_years(start_date, end_date);
        }

        return years;
    }

    //CALCULATE FATHERS AGE AT DELIVERY ON BC helper
    public static string calc_years(DateTime p_start_date, DateTime p_end_date)
    {
        var years = "";

        var age = p_end_date.Year - p_start_date.Year;
        if (p_end_date.DayOfYear < p_start_date.DayOfYear)
            age = age - 1;

        years = age.ToString();

        return years;
    }

    /*"1 --> dcdi_doi_hospi = 0 and dcdi_doo_hospi = 9999 (blank)
        2 --> dcdi_doi_hospi = 1 and dcdi_doo_hospi = 9999 (blank)
        3 --> dcdi_doi_hospi = 2 and dcdi_doo_hospi = 9999 (blank)
        4 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 2
        5 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 0
        6 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 1 
        7 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 3
        9 --> dcdi_doi_hosp = 7777 (unknown) and dcdi_doo_hospi = 7777 (unknown) "
            */
    public static string DPLACE_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "9999";
                break;
            case "5":
                value = "9999";
                break;
            case "6":
                value = "9999";
                break;
            case "7":
                value = "9999";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    /*"1 --> dcdi_doi_hospi = 0 and dcdi_doo_hospi = 9999 (blank)
        2 --> dcdi_doi_hospi = 1 and dcdi_doo_hospi = 9999 (blank)
        3 --> dcdi_doi_hospi = 2 and dcdi_doo_hospi = 9999 (blank)
        4 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 2
        5 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 0
        6 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 1 
        7 --> dcdi_doi_hospi = 9999 (blank) and dcdi_doo_hospi = 3
        9 --> dcdi_doi_hosp = 7777 (unknown) and dcdi_doo_hospi = 7777 (unknown) "
            */
    public static string DPLACE_Outside_of_hospital_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "1":
                value = "9999";
                break;
            case "2":
                value = "9999";
                break;
            case "3":
                value = "9999";
                break;
            case "4":
                value = "2";
                break;
            case "5":
                value = "0";
                break;
            case "6":
                value = "1";
                break;
            case "7":
                value = "3";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    //Map to MMRIA field via Merge with other place of death street fields(STNUM_D, PREDIR_D, STNAME_D, STDESIG_D, POSTDIR_D) 1 of 5
    public static string PLACE_OF_LAST_RESIDENCE_street_Rule(string stnum_r, string predir_r, string stname_r, string stdesig_r, string postdir_r)
    {
        return MergeStreetAddressParts(stnum_r, predir_r, stname_r, stdesig_r, postdir_r);
    }

    //Map to MMRIA field via Merge with other place of death street fields(STNUM_D, PREDIR_D, STNAME_D, STDESIG_D, POSTDIR_D) 1 of 5
    public static string ADDRESS_OF_DEATH_street_Rule(string stnum_d, string predir_d, string stname_d, string stdesig_d, string postdir_d)
    {
        return MergeStreetAddressParts(stnum_d, predir_d, stname_d, stdesig_d, postdir_d);
    }

    private static string MergeStreetAddressParts(string streetNumber, string preDirection, string streetName, string streetDesignator, string postDirection)
    {
        var normalizedStreetNumber = streetNumber?.Trim() ?? string.Empty;
        var normalizedStreetName = streetName?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(normalizedStreetNumber) && !string.IsNullOrWhiteSpace(normalizedStreetName))
        {
            if (normalizedStreetName.Equals(normalizedStreetNumber, StringComparison.OrdinalIgnoreCase))
            {
                normalizedStreetName = string.Empty;
            }
            else if (normalizedStreetName.StartsWith(normalizedStreetNumber + " ", StringComparison.OrdinalIgnoreCase))
            {
                normalizedStreetName = normalizedStreetName.Substring(normalizedStreetNumber.Length).Trim();
            }
        }

        var parts = new List<string>(5);

        AddStreetPart(parts, normalizedStreetNumber);
        AddStreetPart(parts, preDirection);
        AddStreetPart(parts, normalizedStreetName);
        AddStreetPart(parts, streetDesignator);
        AddStreetPart(parts, postDirection);

        return string.Join(" ", parts);
    }

    private static void AddStreetPart(List<string> parts, string value)
    {
        var normalized = value?.Trim();

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            parts.Add(normalized);
        }
    }

    //"Combine RACE22 and RACE23 into one field (dcr_o_race), separated by pipe delimiter.
    //1.Transfer string verbatim from RACE22 to MMRIA field.
    //2.Transfer string verbatim from RACE23 and add to same MMRIA field.
    //3.If both RACE22 and RACE23 are empty, leave MMRIA field empty(blank)."
    public static string RACE_other_race_Rule(string race22, string race23)
    {
        string determinedValue = string.Empty;

        if (!string.IsNullOrWhiteSpace(race22) && !string.IsNullOrWhiteSpace(race23))
            determinedValue = $"{race22}|{race23}";
        else if (!string.IsNullOrWhiteSpace(race22))
            determinedValue = race22;
        else if (!string.IsNullOrWhiteSpace(race23))
            determinedValue = race23;

        return determinedValue;
    }

    //"Combine RACE20 and RACE21 into one field (dcr_op_islan), separated by pipe delimiter.
    //1.Transfer string verbatim from RACE20 to MMRIA field.
    //2.Transfer string verbatim from RACE21 and add to same MMRIA field.
    //3.If both RACE20 and RACE21 are empty, leave MMRIA field empty(blank)."
    public static string RACE_other_pacific_islander_Rule(string race20, string race21)
    {
        string determinedValue = string.Empty;

        if (!string.IsNullOrWhiteSpace(race20) && !string.IsNullOrWhiteSpace(race21))
            determinedValue = $"{race20}|{race21}";
        else if (!string.IsNullOrWhiteSpace(race20))
            determinedValue = race20;
        else if (!string.IsNullOrWhiteSpace(race21))
            determinedValue = race21;

        return determinedValue;
    }

    //"Combine RACE18 and RACE19 into one field (dcr_o_asian), separated by pipe delimiter.
    //1.Transfer string verbatim from RACE18 to MMRIA field.
    //2.Transfer string verbatim from RACE19 and add to same MMRIA field.
    //3.If both RACE18 and RACE19 are empty, leave MMRIA field empty(blank)."
    //Defaulting to blank
    public static string RACE_other_asian_Rule(string race18, string race19)
    {
        string determinedValue = string.Empty;

        if (!string.IsNullOrWhiteSpace(race18) && !string.IsNullOrWhiteSpace(race19))
            determinedValue = $"{race18}|{race19}";
        else if (!string.IsNullOrWhiteSpace(race18))
            determinedValue = race18;
        else if (!string.IsNullOrWhiteSpace(race19))
            determinedValue = race19;

        return determinedValue;

    }

    //"Combine RACE16 and RACE17 into one field (dcr_p_tribe), separated by pipe delimiter.
    //1.Transfer string verbatim from RACE16 to MMRIA field.
    //2.Transfer string verbatim from RACE17 and add to same MMRIA field.
    //3.If both RACE16 and RACE17 are empty, leave MMRIA field empty(blank)."
    //Defaulting to blank
    public static string RACE_Principal_Tribe_Rule(string race16, string race17)
    {
        string determinedValue = string.Empty;

        if (!string.IsNullOrWhiteSpace(race16) && !string.IsNullOrWhiteSpace(race17))
            determinedValue = $"{race16}|{race17}";
        else if (!string.IsNullOrWhiteSpace(race16))
            determinedValue = race16;
        else if (!string.IsNullOrWhiteSpace(race17))
            determinedValue = race17;

        return determinedValue;
    }

    //"Use values from RACE1 through RACE15 to populate MMRIA multi-select field (dcr_race).
    //If every one of RACE1 through RACE15 is equal to ""N"", then dcr_race = 8888(Race Not Specified)"
    //"Use values from RACE1 through RACE15 to populate MMRIA multi-select field (dcr_race).
    //RACE1 = Y-- > dcr_race = 0
    //RACE2 = Y-- > dcr_race = 1
    //RACE3 = Y-- > dcr_race = 2
    //RACE4 = Y-- > dcr_race = 7
    //RACE5 = Y-- > dcr_race = 8
    //RACE6 = Y-- > dcr_race = 9
    //RACE7 = Y-- > dcr_race = 10
    //RACE8 = Y-- > dcr_race = 11
    //RACE9 = Y-- > dcr_race = 12
    //RACE10 = Y-- > dcr_race = 13
    //RACE11 = Y-- > dcr_race = 3
    //RACE12 = Y-- > dcr_race = 4
    //RACE13 = Y-- > dcr_race = 5
    //RACE14 = Y-- > dcr_race = 6
    //RACE15 = Y-- > dcr_race = 14

    //Defaulting to blank
    public static string[] RACE_Rule(string race1, string race2, string race3,
        string race4, string race5, string race6,
        string race7, string race8, string race9,
        string race10, string race11, string race12,
        string race13, string race14, string race15)
    {
        List<string> determinedValues = new List<string>();

        if (race1 == "N" && race2 == "N" && race3 == "N" && race4 == "N"
            && race5 == "N" && race6 == "N" && race7 == "N" && race8 == "N"
            && race9 == "N" && race10 == "N" && race11 == "N" && race12 == "N"
            && race13 == "N" && race14 == "N" && race15 == "N")
            determinedValues.Add("8888");
        else
        {
            if (race1 == "Y")
                determinedValues.Add("0");

            if (race2 == "Y")
                determinedValues.Add("1");

            if (race3 == "Y")
                determinedValues.Add("2");

            if (race4 == "Y")
                determinedValues.Add("7");

            if (race5 == "Y")
                determinedValues.Add("8");

            if (race6 == "Y")
                determinedValues.Add("9");

            if (race7 == "Y")
                determinedValues.Add("10");

            if (race8 == "Y")
                determinedValues.Add("11");

            if (race9 == "Y")
                determinedValues.Add("12");

            if (race10 == "Y")
                determinedValues.Add("13");

            if (race11 == "Y")
                determinedValues.Add("3");

            if (race12 == "Y")
                determinedValues.Add("4");

            if (race13 == "Y")
                determinedValues.Add("5");

            if (race14 == "Y")
                determinedValues.Add("6");

            if (race15 == "Y")
                determinedValues.Add("14");
        }

        return determinedValues.ToArray();
    }

    //"Use values of DETHNIC1, DETHNIC2, DETHNIC3, DETHNIC4 to fill out MMRIA field dcd_ioh_origi.
    //If DETHNIC1 = N and DETHNIC2 = N and DETHNIC3 = N and DETHNIC 4 = N-- > dcd_ioh_origi = 0 No, Not Spanish/ Hispanic / Latino
    //If DETHNIC1 = U and DETHNIC2 = U and DETHNIC3 = U and DETHNIC4 = U-- > dcd_ioh_origi = 7777 Unknown
    //If DETHNIC1 = (empty)and DETHNIC2 = (empty)and DETHNIC3 = (empty)and DETHNIC4 = (empty)-- > dcd_ioh_origi = 9999(blank)"
    //H-- > dcd_ioh_origi = 1 Yes, Mexican, Mexican American, Chicano
    //H-- > dcd_ioh_origi = 2 Yes, Puerto Rican
    //H-- > dcd_ioh_origi = 3 Yes, Cuban
    //H-- > dcd_ioh_origi = 4 Yes, Other Spanish/ Hispanic / Latino

    //Defaulting to blank
    public static string DETHNIC_Rule(string value1, string value2, string value3, string value4)
    {
        string determinedValue = "9999";

        if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N")
            determinedValue = "0";
        else if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U")
            determinedValue = "7777";
        else if (value1 == "H")
            determinedValue = "1";
        else if (value2 == "H")
            determinedValue = "2";
        else if (value3 == "H")
            determinedValue = "3";
        else if (value4 == "H")
            determinedValue = "4";

        return determinedValue;
    }

    //"Map character to MMRIA code values as follows:
    //Blank fields -> 9999(blank)
    //N-> 0 Natural
    //A-> 2 Accident
    //S-> 3 Suicide
    //H-> 1 Homicide
    //P-> 5 Pending Investigation
    //C-> 6 Could Not Be Determined

    //Map empty rows-- > 9999(blank)"
    public static string MANNER_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "N":
                value = "0";
                break;
            case "A":
                value = "2";
                break;
            case "S":
                value = "3";
                break;
            case "H":
                value = "1";
                break;
            case "P":
                value = "5";
                break;
            case "C":
                value = "6";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    //"Map character to MMRIA code values as follows:
    //Blank fields -> 9999(blank)
    //Y-> 1 = Yes
    //N-> 0 = No
    //U->  7777 = Unknown
    //"
    public static string AUTOP_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    //"Map character to MMRIA code values as follows:
    //Blank fields -> 9999(blank)
    //Y-> 1 = Yes
    //N-> 0 = No
    //U->  7777 = Unknown
    //"
    public static string AUTOPF_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    //"Map character to MMRIA code values as follows:
    //Blank fields -> 9999(blank)
    //Y-> 1 = Yes
    //N-> 0 = No
    //P-> 2 = Probably
    //U-> 7777 = Unknown
    //C-> 7777 = Unknown"
    public static string TOBAC_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "P":
                value = "2";
                break;
            case "U":
            case "C":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    //"Map number to MMRIA number codes as follows:
    //Empty columns -> 9999 = (blank)
    //1-- > 0 Not pregnant within last year
    //2-- > 1 Pregnant at the time of death
    //3-- > 2 Pregnant within 42 days of death
    //4-- > 3 Pregnant within 43 to 365 days of death
    //8-- > 5 Not Applicable
    //9-- > 88 Unknown if pregnant in last year "
    public static string PREG_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "8":
                value = "5";
                break;
            case "9":
                value = "88";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    //Transfer number verbatim to MMRIA field; Map 99 and blank -> 9999(blank)
    public static string DOI_MO_Rule(string value)
    {
        if (value == "99" || string.IsNullOrWhiteSpace(value))
            value = "9999";

        return value;
    }

    //Transfer number verbatim to MMRIA field; Map 99 and blank -> 9999(blank)
    public static string DOI_DY_Rule(string value)
    {
        if (value == "99" || string.IsNullOrWhiteSpace(value))
            value = "9999";

        return value;
    }

    //Transfer number verbatim to MMRIA field; Map 9999 and blank ->9999(blank)
    public static string DOI_YR_Rule(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            value = "9999";

        return value;
    }

    //Transfer number verbatim to MMRIA field; Values of 9999 and blank should be mapped as blank; need to map these values to MMRIA time format
    public static string TOI_HR_Rule(string value)
    {
        string parsedValue = "";

        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value == "9999")
                parsedValue = "";
            else
            {
                parsedValue = ConvertHHmm_To_MMRIATime(value);
            }
        }

        return parsedValue;
    }

    //"Map character to MMRIA code values as follows:
    //Blank fields -> 9999(blank)
    //Y-> 1 = Yes
    //N-> 0 = No
    //U->  7777 = Unknown
    //"
    public static string WORKINJ_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    //"Map character to MMRIA code values as follows:
    //Blank fields -> 9999(blank)
    //Y-> 1 = Yes
    //N-> 0 = No
    //U->  7777 = Unknown
    //"
    public static string ARMEDF_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    //Transfer string verbatim to MMRIA field; map values of 99999 to blank
    public static string ZIP9_D_Rule(string value)
    {
        if (value == "99999")
            value = string.Empty;

        return value;
    }

    //"1. Map character to MMRIA code values as follows:
    //Blank fields -> 9999(blank)
    //DR-> 0 = Driver / Operator
    //PA-> 1 = Passenger
    //PE-> 2 = Pedestrian
    //Map any other text -> 3 = Other
    //2.Map full text to MMRIA Specify Other field"
    public static string TRANSPRT_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "DR":
                value = "0";
                break;
            case "PA":
                value = "1";
                break;
            case "PE":
                value = "2";
                break;
            case "":
                value = "9999";
                break;
            default:
                value = "3";
                break;
        }

        return value;
    }

    //"1. Map character to MMRIA code values as follows:
    //Blank fields -> 9999(blank)
    //DR-> 0 = Driver / Operator
    //PA-> 1 = Passenger
    //PE-> 2 = Pedestrian
    //Map any other text -> 3 = Other
    //2.Map full text to MMRIA Specify Other field"
    public static string TRANSPRT_other_specify_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "DR":
                value = "";
                break;
            case "PA":
                value = "";
                break;
            case "PE":
                value = "";
                break;
            case "":
                value = "";
                break;
            default:
                value = value;
                break;
        }

        return value;
    }

    //3-> 3 = N / A(identified via linkage or literal cause of death field)        9999-> 9999(blank)
    public static string VRO_STATUS_Rule(string value)
    {
        if (value == "9999")
            value = string.Empty;

        return value;
    }

    //Map number to MMRIA number codes as follows:
    //Empty columns -> 9999 = (blank)
    //1-> 0 = 8th Grade or Less
    //2-> 1 = 9th - 12th grade; No Diploma
    //3-> 2 = High School Graduate or GED Completed
    //4-> 3 = Some college credit, but no degree
    //5-> 4 = Associate Degree
    //6-> 5 = Bachelor's Degree
    //7-> 6 = Master's Degree
    //8-> 7 = Doctorate Degree or Professional Degree
    //9-> 7777 = Unknown
    public static string DEDUC_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "6":
                value = "5";
                break;
            case "7":
                value = "6";
                break;
            case "8":
                value = "7";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    //Transfer number verbatim to MMRIA field, format as MMRIA time.; if TOD = 9999 then this field should be left blank
    public static string TOD_Rule(string value)
    {
        string parsedValue = "";

        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value == "9999")
                parsedValue = "";
            else
            {
                parsedValue = ConvertHHmm_To_MMRIATime(value);

            }
        }

        return parsedValue;
    }

    //Transfer number verbatim to MMRIA field; if DOD_DY = 99 then this field should be mapped to 9999(blank)
    public static string DOD_DY_Rule(string value)
    {
        if (value == "99")
            value = "9999";

        return value;
    }

    //Transfer number verbatim to MMRIA field; if DOD_MO = 99 then this field should be mapped to 9999(blank)
    public static string DOD_MO_Rule(string value)
    {
        if (value == "99")
            value = "9999";

        return value;
    }

    //Map character to MMRIA number codes as follows:
    //Blank-> 9999 = (blank)
    //M-> 0 = Married
    //A-> 1 = Married, but Separated
    //W-> 2 = Widowed
    //D-> 3 = Divorced
    //S-> 4 = Never Married
    //U->  7777 = Unknown
    public static string MARITAL_Rule(string value)
    {
        switch (value?.ToUpper())
        {
            case "M":
                value = "0";
                break;
            case "A":
                value = "1";
                break;
            case "W":
                value = "2";
                break;
            case "D":
                value = "3";
                break;
            case "S":
                value = "4";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    //Map to MMRIA field Country listing
    //Map XX to 9999(blank)
    //Map ZZ to 9999(blank)
    public static string COUNTRYC_Rule(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "XX" || value == "ZZ")
            value = "9999";

        return value;

    }

    // Map to MMRIA field state listing.
    //Map XX to 9999(blank)
    public static string STATEC_Rule(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    // Map to MMRIA field state listing.
    //Map XX to 9999(blank)
    public static string BPLACE_ST_Rule(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    //Transfer number verbatim to MMRIA field; IF value='99', this field should be mapped to 9999 (blank)
    public static string DOB_DY_Rule(string value)
    {
        if (value == "99")
            value = "9999";

        return value;
    }

    //Transfer number verbatim to MMRIA field; IF value='99', this field should be mapped to 9999 (blank)
    public static string DOB_MO_Rule(string value)
    {
        if (value == "99")
            value = "9999";

        return value;
    }

    //Transfer number verbatim to MMRIA field; IF AGE = 999 this field should be left blank
    public static string AGE_Rule(string value)
    {
        if (value == "999")
            value = string.Empty;

        return value;
    }

    //Transfer string verbatim to MMRIA field; empty fields should map to 9999(blank)
    public static string DOD_YR_Rule(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            value = "9999";

        return value;
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

    public static object NAT_maternal_morbidity_Rule(string value1, string value2, string value3, string value4, string value5, string value6)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        MTR = Y --> bfdcp_m_morbi = 0 Maternal transfusion

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N")
        //    determinedValues.Add("6");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

        }

        return determinedValues.ToArray();
    }

    public static object NAT_characteristics_of_labor_and_delivery_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7, string value8, string value9)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

INDL = Y --> bfdcp_cola_deliv = 0 Induction of labor

If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //     && value9 == "N")
        //    determinedValues.Add("9");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
                && value9 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);
        }

        return determinedValues.ToArray();
    }

    public static object NAT_onset_of_labor_Rule(string value1, string value2, string value3)
    {
        /*Use values from 3 IJE fields [PROM, PRIC, PROL] to populate MMRIA multi-select field (bfdcp_oo_labor). 

PROM = Y --> bfdcp_oo_labor = 0 Premature Rupture of Membranes (Prolonged)

If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "N", then bfdcp_oo_labor = 3 None of the above

If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "U" then bfdcp_oo_labor = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N")
        //    determinedValues.Add("3");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

        }

        return determinedValues.ToArray();
    }

    public static object NAT_obstetric_procedures_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

CERV = Y --> bfdcp_o_proce = 0 Cervical Cerclage

If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N")
        //    determinedValues.Add("4");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

        }

        return determinedValues.ToArray();
    }

    public static object NAT_infections_present_or_treated_during_pregnancy_Rule(string value1, string value2, string value3, string value4, string value5, string value6)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

GON = Y --> bfdcp_ipotd_pregn = 2 Gonorrhea

If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N")
        //    determinedValues.Add("10");
        //else
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

        }

        return determinedValues.ToArray();
    }

    public static object NAT_risk_factors_in_this_pregnancy_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7, string value8, string value9, string value10, string value11)
    {
        //    /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        //   EHYPE = Y --> bfdcprf_rfit_pregn = 4 Eclampsia Hypertension

/*
                                field_set["PDIAB"],
                                field_set["GDIAB"],
                                field_set["PHYPE"],
                                field_set["GHYPE"],
                                field_set["PPB"],
                                field_set["PPO"],
                                field_set["INFT"],
                                field_set["PCES"],
                                field_set["EHYPE"],
                                field_set["INFT_DRG"],
                                field_set["INFT_ART"]
*/


        //   If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        //   If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        //   *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */

        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //    && value9 == "N")
        //    determinedValues.Add("11");
        //else 
        if 
        (
            value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
            && value9 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);

            if (int.TryParse(value10, out result))
                determinedValues.Add(value10);


            if (int.TryParse(value11, out result))
                determinedValues.Add(value11);
        }

        return determinedValues.ToArray();
    }

    public static object NAT_congenital_Rule(string value1, string value2, string value3, string value4, string value5
        , string value6, string value7, string value8, string value9
        , string value10, string value11, string value12)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/

        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //     && value9 == "N" && value10 == "N" && value11 == "N" && value12 == "N")
        //    determinedValues.Add("17");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
                && value9 == "U" && value10 == "U" && value11 == "U" && value12 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);

            if (int.TryParse(value10, out result))
                determinedValues.Add(value10);

            if (int.TryParse(value11, out result))
                determinedValues.Add(value11);

            if (int.TryParse(value12, out result))
                determinedValues.Add(value12);
        }

        return determinedValues.ToArray();
    }

    public static object NAT_abnormal_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N")
        //    determinedValues.Add("8");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);
        }

        return determinedValues.ToArray();
    }

    public static string LOCATION_OF_RESIDENCE_street_Rule(string stnum_r, string predir_r, string stname_r, string stdesig_r, string postdir_r)
    {
        //Map to MMRIA field via Merge with other place of death street fields(STNUM_D, PREDIR_D, STNAME_D, STDESIG_D, POSTDIR_D) 1 of 5
        string determinedValue = $"{stnum_r} {predir_r} {stname_r} {stdesig_r} {postdir_r}";

        return determinedValue;
    }

    public static string DATE_OF_DELIVERY_Rule(string year, string month, string day)
    {
        //2.Merge 3 fields(IDOB_MO, IDOB_DY, IDOB_YR) map resulting date to MMRIA field -date_of _delivery(bcifsri_do_deliv)."
        string determinedValue = $"{year}-{month}-{day}";

        return determinedValue;
    }

    public static string IDOB_YR_Merge_Rule(string value)
    {
        /*1. Transfer number verbatim to MMRIA field - date_of_delivery/year (bfdcpfodddod_year)
        2. Merge 3 fields (IDOB_MO, IDOB_DY, IDOB_YR) map resulting date to MMRIA field - date_of _delivery (bcifsri_do_deliv).*/
        return value;
    }

    public static string MDOB_YR_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/

        if (value == "9999")
            value = "9999";

        return value;
    }

    public static string MDOB_MO_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    public static string MDOB_DY_Rule(string value)
    {
        /*If value is in 01-31, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    public static string FDOB_YR_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/

        if (value == "9999")
            value = "9999";

        return value;
    }

    public static string FDOB_MO_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    public static string NAT_STATEC_Rule(string value)
    {
        //"Map XX --> 9999 (blank)
        //Map ZZ --> 9999(blank)
        //Map all other values to MMRIA field state listing"

        if (string.IsNullOrWhiteSpace(value) || value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    public static string MARN_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string ACKN_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        X -> 2=Not Applicable
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            case "X":
                value = "2";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string MEDUC_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = 8th Grade or Less
        2  -> 1 = 9th-12th Grade; No Diploma
        3  -> 2 = High School Grad or GED Completed 
        4  -> 3 = Some college, but no degree
        5  -> 4 = Associate Degree
        6  -> 5 = Bachelor's Degree
        7  -> 6 = Master's Degree
        8  -> 7 = Doctorate or Professional Degree
        9  -> 7777 = Unknown*/


        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "6":
                value = "5";
                break;
            case "7":
                value = "6";
                break;
            case "8":
                value = "7";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string FEDUC_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = 8th Grade or Less
        2  -> 1 = 9th-12th Grade; No Diploma
        3  -> 2 = High School Grad or GED Completed 
        4  -> 3 = Some college, but no degree
        5  -> 4 = Associate Degree
        6  -> 5 = Bachelor's Degree
        7  -> 6 = Master's Degree
        8  -> 7 = Doctorate or Professional Degree
        9  -> 7777 = Unknown*/


        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "6":
                value = "5";
                break;
            case "7":
                value = "6";
                break;
            case "8":
                value = "7";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string ATTEND_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = MD
        2 -> 1 = DO
        3 -> 2 = CNM/CM
        4 -> 3 = Other Midwife
        5 -> 4 = Other 
        9 -> 7777 = Unknown*/


        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string TRAN_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string NPREV_Rule(string value)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field. 

        If value = 99, map to 9999 (blank)*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string HFT_Rule(string value)
    {
        /*If value is in 1-8, transfer number verbatim to MMRIA field. 

        If value = 9, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "9")
            value = "";

        return value;
    }

    public static string HIN_Rule(string value)
    {
        /*If value is in 00-11, transfer number verbatim to MMRIA field. 

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string PWGT_Rule(string value)
    {
        /*If value is in 050-400, transfer number verbatim to MMRIA field.

        If value = 999, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "999" || value == "9999")
            value = "";

        return value;
    }

    public static string DWGT_Rule(string value)
    {
        /*If value is in 050-450, transfer number verbatim to MMRIA field.  

        If value = 999, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "999" || value == "9999")
            value = "";

        return value;
    }

    public static string WIC_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string PLBL_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.  

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string PLBD_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.  

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string POPO_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99" || value == "9999")
            value = "";

        return value;
    }

    public static string MLLB_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 88 or 99, map to 9999 (blank).*/

        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    public static string YLLB_Rule(string value)
    {
        /*If value is not 8888 or 9999, transfer number verbatim to MMRIA field.

        If value = 8888 or 9999, map to 9999 (blank).*/

        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    public static string MOPO_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 88 or 99, map to 9999 (blank).*/

        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    public static string YOPO_Rule(string value)
    {
        /*If value is not 8888 or 9999, transfer number verbatim to MMRIA field.  

        If value = 8888 or 9999, map to 9999 (blank).*/

        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    public static string PAY_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 1 = Medicaid
        2 -> 0 = Private Insurance
        3 -> 2 = Self-pay                                       
        4 -> 4=Indian Health Service                     
        5 -> 5=CHAMPUS/TRICARE                               
        6 -> 6 = Other Government (Fed, State, Local)
        8 -> 3 = Other                                          
        9 -> 7777=Unknown*/
        switch (value?.ToUpper())
        {
            case "1":
                value = "1";
                break;
            case "2":
                value = "0";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "4";
                break;
            case "5":
                value = "5";
                break;
            case "6":
                value = "6";
                break;
            case "8":
                value = "3";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string DLMP_YR_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/

        if (value == "9999")
            value = "9999";

        return value;
    }

    public static string DLMP_MO_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    public static string DLMP_DY_Rule(string value)
    {
        /*If value is in 01-31, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    public static string NPCES_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.  

        If value = 99, leave the value empty/blank.*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string ATTF_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */

        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string ATTV_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  -> 7777 = Unknown
        */

        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string PRES_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = Cephalic
        2 -> 1 = Breech
        3 -> 4 = Other
        9 -> 7777 = Unknown*/


        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "4";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string ROUT_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = Vaginal/Spontaneous
        2 -> 1 = Vaginal/Forceps
        3  -> 2 = Vaginal/Vacuum
        4  -> 3 = Cesarean
        9  -> 7777 = Unknown*/


        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "9":
                value = "7";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string OWGEST_Rule(string value)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the value empty/blank. */

        if (value == "99")
            value = "";

        return value;
    }

    public static string APGAR5_Rule(string value)
    {
        /*If value is in 00-10, transfer number verbatim to MMRIA field.  

        If value = 99, leave the value empty/blank. */

        if (value == "99")
            value = "";

        return value;
    }

    public static string APGAR10_Rule(string value)
    {
        /*If value is in 00-10, transfer number verbatim to MMRIA field.  

        If value = 88 or 99, leave the value empty/blank. */

        if (value == "88" || value == "99")
            value = "";

        return value;
    }

    public static string SORD_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.  

        If value = 99, leave the MMRIA value empty/blank.*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string ITRAN_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 Yes
        N  -> 0 No
        U  -> 7777 = Unknown
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string ILIV_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 = Yes
        N  -> 0 = No
        U  -> 2 = Infant transferred, status unknown
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "2";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string BFED_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 Yes
        N  -> 0 No
        U  -> 7777 = Unknown
        */


        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string ISEX_NAT_Rule(string value)
    {
        /*M = Male -> 0:Male
        F = Female -> 1:Female
        N = 2:Not Yet Determined

        Map empty rows to 9999 (blank)
        */

        switch (value?.ToUpper())
        {
            case "M":
                value = "0";
                break;
            case "F":
                value = "1";
                break;
            case "N":
                value = "2";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }
    public static string BPLACE_place_NAT_Rule(string value)
    {
        /*1 = Hospital -> bfdcpfodd_to_place = 0 Hospital & bfdcpfodd_whd_plann = 9999 (blank)

        2 = Freestanding Birth Center -> bfdcpfodd_to_place = 1 Free Standing Birth Center & bfdcpfodd_whd_plann = 9999 (blank)

        3 = Home (Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 1 Yes

        4 = Home (Not Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 0 No

        5 = Home (Unknown if Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 7777 Unknown

        6 = Clinic/Doctor's Office -> bfdcpfodd_to_place = 3 Clinic/Doctor's office & bfdcpfodd_whd_plann = 9999 (blank)

        7 = Other -> bfdcpfodd_to_place = 4 Other & bfdcpfodd_whd_plann = 9999 (blank)

        9 = Unknown --> bfdcpfodd_to_place = 7777 Unknown & bfdcpfodd_whd_plann = 9999 (blank)*/
        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "2";
                break;
            case "5":
                value = "2";
                break;
            case "6":
                value = "3";
                break;
            case "7":
                value = "4";
                break;
            default:
                value = "7777";
                break;
        }
        return value;
    }
    public static string BPLACE_plann_NAT_Rule(string value)
    {
        /*1 = Hospital -> bfdcpfodd_to_place = 0 Hospital & bfdcpfodd_whd_plann = 9999 (blank)

        2 = Freestanding Birth Center -> bfdcpfodd_to_place = 1 Free Standing Birth Center & bfdcpfodd_whd_plann = 9999 (blank)

        3 = Home (Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 1 Yes

        4 = Home (Not Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 0 No

        5 = Home (Unknown if Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 7777 Unknown

        6 = Clinic/Doctor's Office -> bfdcpfodd_to_place = 3 Clinic/Doctor's office & bfdcpfodd_whd_plann = 9999 (blank)

        7 = Other -> bfdcpfodd_to_place = 4 Other & bfdcpfodd_whd_plann = 9999 (blank)

        9 = Unknown --> bfdcpfodd_to_place = 7777 Unknown & bfdcpfodd_whd_plann = 9999 (blank)*/
        switch (value?.ToUpper())
        {
            case "1":
                value = "9999";
                break;
            case "2":
                value = "9999";
                break;
            case "3":
                value = "1";
                break;
            case "4":
                value = "0";
                break;
            case "5":
                value = "7777";
                break;
            case "6":
                value = "9999";
                break;
            case "7":
                value = "9999";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }
    public static string BPLACEC_ST_TER_NAT_Rule(string value)
    {
        /*Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)

        Map all other values to MMRIA field state listing*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    public static string NAT_METHNIC_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values of METHNIC1, METHNIC2, METHNIC3, METHNIC4 to populate MMRIA field bfdcpdom_ioh_origi.

        H --> bfdcpdom_ioh_origi = 1 Yes, Mexican, Mexican American, Chicano
        H --> bfdcpdom_ioh_origi = 2 Yes, Puerto Rican
        H --> bfdcpdom_ioh_origi = 3 Yes, Cuban
        H --> bfdcpdom_ioh_origi = 4 Yes, Other Spanish/Hispanic/Latino

        If METHNIC1 = N and METHNIC2 = N and METHNIC3 = N and METHNIC 4 = N --> bfdcpdom_ioh_origi = 0 No, Not Spanish/Hispanic/Latino

        If METHNIC1 = U and METHNIC2 = U and METHNIC3 = U and METHNIC4 = U --> bfdcpdom_ioh_origi = 7777 Unknown

        If METHNIC1 = (empty) and METHNIC2 = (empty) and METHNIC3 = (empty) and METHNIC4 = (empty) --> bfdcpdom_ioh_origi = 9999 (blank)*/

        string determinedValue;

        if (value1?.ToUpper() == "H")
        {
            determinedValue = "1";
        }
        else if (value2?.ToUpper() == "H")
        {
            determinedValue = "2";
        }
        else if (value3?.ToUpper() == "H")
        {
            determinedValue = "3";
        }
        else if (value4?.ToUpper() == "H")
        {
            determinedValue = "4";
        }
        else if (value1?.ToUpper() == "N" && value2?.ToUpper() == "N" && value3?.ToUpper() == "N" && value4?.ToUpper() == "N")
        {
            determinedValue = "0";
        }
        else if (value1?.ToUpper() == "U" && value2?.ToUpper() == "U" && value3?.ToUpper() == "U" && value4?.ToUpper() == "U")
        {
            determinedValue = "7777";
        }
        else
        {
            determinedValue = "9999";
        }

        return determinedValue;
    }

    public static string[] MRACE_NAT_Rule(string value1, string value2, string value3, string value4, string value5,
        string value6, string value7, string value8, string value9, string value10,
        string value11, string value12, string value13, string value14, string value15)
    {
        /*Use values from MRACE1 through MRACE15 to populate MMRIA multi-select field (bfdcpr_ro_mothe).

        MRACE1 = Y --> bfdcpr_ro_mothe = 0 White
        MRACE2 = Y --> bfdcpr_ro_mothe = 1 Black or African American
        MRACE3 = Y --> bfdcpr_ro_mothe = 2 American Indian or Alaska Native
        MRACE4 = Y --> bfdcpr_ro_mothe = 7 Asian Indian
        MRACE5 = Y --> bfdcpr_ro_mothe = 8 Chinese
        MRACE6 = Y --> bfdcpr_ro_mothe = 9 Filipino
        MRACE7 = Y --> bfdcpr_ro_mothe = 10 Japanese
        MRACE8 = Y --> bfdcpr_ro_mothe = 11 Korean
        MRACE9 = Y --> bfdcpr_ro_mothe = 12 Vietnamese
        MRACE10 = Y --> bfdcpr_ro_mothe = 13 Other Asian
        MRACE11 = Y --> bfdcpr_ro_mothe = 3 Native Hawaiian
        MRACE12 = Y --> bfdcpr_ro_mothe = 4 Guamanian or Chamorro
        MRACE13 = Y --> bfdcpr_ro_mothe = 5 Samoan
        MRACE14 = Y --> bfdcpr_ro_mothe = 6 Other Pacific Islander
        MRACE15 = Y --> bfdcpr_ro_mothe = 14 Other Race

        If every one of MRACE1 through MRACE15 is equal to "N", then bfdcpr_ro_mothe = 8888 (Race Not Specified)*/
        //Defaulting to blank
        List<string> determinedValues = new List<string>();

        if (value1?.ToUpper() == "Y")
        {
            determinedValues.Add("0");
        }
        if (value2?.ToUpper() == "Y")
        {
            determinedValues.Add("1");
        }
        if (value3?.ToUpper() == "Y")
        {
            determinedValues.Add("2");
        }
        if (value4?.ToUpper() == "Y")
        {
            determinedValues.Add("7");
        }
        if (value5?.ToUpper() == "Y")
        {
            determinedValues.Add("8");
        }
        if (value6?.ToUpper() == "Y")
        {
            determinedValues.Add("9");
        }
        if (value7?.ToUpper() == "Y")
        {
            determinedValues.Add("10");
        }
        if (value8?.ToUpper() == "Y")
        {
            determinedValues.Add("11");
        }
        if (value9?.ToUpper() == "Y")
        {
            determinedValues.Add("12");
        }
        if (value10?.ToUpper() == "Y")
        {
            determinedValues.Add("13");
        }
        if (value11?.ToUpper() == "Y")
        {
            determinedValues.Add("3");
        }
        if (value12?.ToUpper() == "Y")
        {
            determinedValues.Add("4");
        }
        if (value13?.ToUpper() == "Y")
        {
            determinedValues.Add("5");
        }
        if (value14?.ToUpper() == "Y")
        {
            determinedValues.Add("6");
        }
        if (value15?.ToUpper() == "Y")
        {
            determinedValues.Add("14");
        }
        if(determinedValues.Count == 0)
        {
            determinedValues.Add("8888");
        }

        return determinedValues.ToArray();
    }

    public static string MRACE16_17_NAT_Rule(string value16, string value17)
    {
        /*Combine MRACE16 and MRACE17 into one field (bfdcpr_p_tribe), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE16 to MMRIA field.
        2. Transfer string verbatim from MRACE17 and add to same MMRIA field.
        3. If both MRACE16 and MRACE17 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value16) && 
            !string.IsNullOrWhiteSpace(value17)
        )
        {
            value = $"{value16}, {value17}";
        }
        else if (!string.IsNullOrWhiteSpace(value16))
        {
            value = $"{value16}";
        }
        else if (!string.IsNullOrWhiteSpace(value17))
        {
            value = $"{value17}";
        }

        return value;
    }

    public static string MRACE18_19_NAT_Rule(string value18, string value19)
    {
        /*Combine MRACE18 and MRACE19 into one field (bfdcpr_o_asian), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE18 to MMRIA field.
        2. Transfer string verbatim from MRACE19 and add to same MMRIA field.
        3. If both MRACE18 and MRACE19 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value18) && 
            !string.IsNullOrWhiteSpace(value19)
        )
        {
            value = $"{value18}, {value19}";
        }
        else if (!string.IsNullOrWhiteSpace(value18))
        {
            value = $"{value18}";
        }
        else if (!string.IsNullOrWhiteSpace(value19))
        {
            value = $"{value19}";
        }

        return value;
    }

    public static string MRACE20_21_NAT_Rule(string value20, string value21)
    {
        /*Combine MRACE20 and MRACE21 into one field (bfdcpr_op_islan), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE20 to MMRIA field.
        2. Transfer string verbatim from MRACE21 and add to same MMRIA field.
        3. If both MRACE20 and MRACE21 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value20) && 
            !string.IsNullOrWhiteSpace(value21)
        )
        {
            value = $"{value20}, {value21}";
        }
        else if (!string.IsNullOrWhiteSpace(value20))
        {
            value = $"{value20}";
        }
        else if (!string.IsNullOrWhiteSpace(value21))
        {
            value = $"{value21}";
        }

        return value;
    }

    public static string MRACE22_23_NAT_Rule(string value22, string value23)
    {
        /*Combine MRACE22 and MRACE23 into one field (bfdcpr_o_race), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE22 to MMRIA field.
        2. Transfer string verbatim from MRACE23 and add to same MMRIA field.
        3. If both MRACE22 and MRACE23 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value22) && 
            !string.IsNullOrWhiteSpace(value23)
        )
        {
            value = $"{value22}, {value23}";
        }
        else if (!string.IsNullOrWhiteSpace(value22))
        {
            value = $"{value22}";
        }
        else if (!string.IsNullOrWhiteSpace(value23))
        {
            value = $"{value23}";
        }

        return value;
    }

    public static string FETHNIC_NAT_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values of FETHNIC1, FETHNIC2, FETHNIC3, FETHNIC4 to populate MMRIA field bfdcpdof_ifoh_origi.

            H --> bfdcpdof_ifoh_origi = 1 Yes, Mexican, Mexican American, Chicano
        H --> bfdcpdof_ifoh_origi = 2 Yes, Puerto Rican
        H --> bfdcpdof_ifoh_origi = 3 Yes, Cuban
        H --> bfdcpdof_ifoh_origi = 4, Yes, Other Spanish/Hispanic/Latino

            If FETHNIC1 = N and FETHNIC2 = N and FETHNIC3 = N and FETHNIC 4 = N --> bfdcpdof_ifoh_origi = 0 No, Not Spanish/Hispanic/Latino

            If FETHNIC1 = U and FETHNIC2 = U and FETHNIC3 = U and FETHNIC4 = U --> bfdcpdof_ifoh_origi = 7777 Unknown

            If FETHNIC1 = (empty) and FETHNIC2 = (empty) and FETHNIC3 = (empty) and FETHNIC4 = (empty) --> bfdcpdof_ifoh_origi = 9999 (blank)*/

        string determinedValue;

        if (value1?.ToUpper() == "H")
        {
            determinedValue = "1";
        }
        else if (value2?.ToUpper() == "H")
        {
            determinedValue = "2";
        }
        else if (value3?.ToUpper() == "H")
        {
            determinedValue = "3";
        }
        else if (value4?.ToUpper() == "H")
        {
            determinedValue = "4";
        }
        else if (value1?.ToUpper() == "N" && value2?.ToUpper() == "N" && value3?.ToUpper() == "N" && value4?.ToUpper() == "N")
        {
            determinedValue = "0";
        }
        else if (value1?.ToUpper() == "U" && value2?.ToUpper() == "U" && value3?.ToUpper() == "U" && value4?.ToUpper() == "U")
        {
            determinedValue = "7777";
        }
        else
        {
            determinedValue = "9999";
        }

        return determinedValue;
    }


    public static string[] FRACE_NAT_Rule(string value1, string value2, string value3, string value4, string value5,
        string value6, string value7, string value8, string value9, string value10,
        string value11, string value12, string value13, string value14, string value15)
    {
        /*Use values from FRACE1 through FRACE15 to populate MMRIA multi-select field (bfdcpdofr_ro_fathe).

        FRACE1 = Y --> bfdcpdofr_ro_fathe = 0 White
        FRACE2 = Y --> bfdcpdofr_ro_fathe = 1 Black or African American
        FRACE3 = Y --> bfdcpdofr_ro_fathe = 2 American Indian or Alaska Native
        FRACE4 = Y --> bfdcpdofr_ro_fathe = 7 Asian Indian
        FRACE5 = Y --> bfdcpdofr_ro_fathe = 8 Chinese
        FRACE6 = Y --> bfdcpdofr_ro_fathe = 9 Filipino
        FRACE7 = Y --> bfdcpdofr_ro_fathe = 10 Japanese
        FRACE8 = Y --> bfdcpdofr_ro_fathe = 11 Korean
        FRACE9 = Y --> bfdcpdofr_ro_fathe = 12 Vietnamese
        FRACE10 = Y --> bfdcpdofr_ro_fathe = 13 Other Asian
        FRACE11 = Y --> bfdcpdofr_ro_fathe = 3 Native Hawaiian
        FRACE12 = Y --> bfdcpdofr_ro_fathe = 4 Guamanian or Chamorro
        FRACE13 = Y --> bfdcpdofr_ro_fathe = 5 Samoan
        FRACE14 = Y --> bfdcpdofr_ro_fathe = 6 Other Pacific Islander
        FRACE15 = Y --> bfdcpdofr_ro_fathe = 14 Other Race

        If every one of FRACE1 through FRACE15 is equal to "N", then bfdcpdofr_ro_fathe = 8888 (Race Not Specified)*/
        List<string> determinedValues = new List<string>();


        if (value1?.ToUpper() == "Y")
        {
            determinedValues.Add("0");
        }
        if (value2?.ToUpper() == "Y")
        {
            determinedValues.Add("1");
        }
        if (value3?.ToUpper() == "Y")
        {
            determinedValues.Add("2");
        }
        if (value4?.ToUpper() == "Y")
        {
            determinedValues.Add("7");
        }
        if (value5?.ToUpper() == "Y")
        {
            determinedValues.Add("8");
        }
        if (value6?.ToUpper() == "Y")
        {
            determinedValues.Add("9");
        }
        if (value7?.ToUpper() == "Y")
        {
            determinedValues.Add("10");
        }
        if (value8?.ToUpper() == "Y")
        {
            determinedValues.Add("11");
        }
        if (value9?.ToUpper() == "Y")
        {
            determinedValues.Add("12");
        }
        if (value10?.ToUpper() == "Y")
        {
            determinedValues.Add("13");
        }
        if (value11?.ToUpper() == "Y")
        {
            determinedValues.Add("3");
        }
        if (value12?.ToUpper() == "Y")
        {
            determinedValues.Add("4");
        }
        if (value13?.ToUpper() == "Y")
        {
            determinedValues.Add("5");
        }
        if (value14?.ToUpper() == "Y")
        {
            determinedValues.Add("6");
        }
        if (value15?.ToUpper() == "Y")
        {
            determinedValues.Add("14");
        }

        if(determinedValues.Count == 0)
        {
            determinedValues.Add("8888");
        }

        return determinedValues.ToArray();
    }

    public static string FRACE16_17_NAT_Rule(string value16, string value17)
    {
        /*Combine FRACE16 and FRACE17 into one field (bfdcpdofr_p_tribe), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE16 to MMRIA field.
        2. Transfer string verbatim from FRACE17 and add to same MMRIA field.
        3. If both FRACE16 and FRACE17 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value16) || string.IsNullOrWhiteSpace(value17)))
        {
            value = $"{value16}|{value17}";
        }
        else if (!string.IsNullOrWhiteSpace(value16))
        {
            value = $"{value16}";
        }
        else
        {
            value = $"{value17}";
        }

        return value;
    }

    public static string FRACE18_19_NAT_Rule(string value18, string value19)
    {
        /*Combine FRACE18 and FRACE19 into one field (bfdcpdofr_o_asian), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE18 to MMRIA field.
        2. Transfer string verbatim from FRACE19 and add to same MMRIA field.
        3. If both FRACE18 and FRACE19 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value18) || string.IsNullOrWhiteSpace(value19)))
        {
            value = $"{value18}|{value19}";
        }
        else if (!string.IsNullOrWhiteSpace(value18))
        {
            value = $"{value18}";
        }
        else
        {
            value = $"{value19}";
        }

        return value;
    }

    public static string FRACE20_21_NAT_Rule(string value20, string value21)
    {
        /*Combine FRACE20 and FRACE21 into one field (bfdcpdofr_op_islan), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE20 to MMRIA field.
        2. Transfer string verbatim from FRACE21 and add to same MMRIA field.
        3. If both FRACE20 and FRACE21 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value20) || string.IsNullOrWhiteSpace(value21)))
        {
            value = $"{value20}|{value21}";
        }
        else if (!string.IsNullOrWhiteSpace(value20))
        {
            value = $"{value20}";
        }
        else
        {
            value = $"{value21}";
        }

        return value;
    }

    public static string FRACE22_23_NAT_Rule(string value22, string value23)
    {
        /*Combine FRACE22 and FRACE23 into one field (bfdcpdofr_o_race), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE22 to MMRIA field.
        2. Transfer string verbatim from FRACE23 and add to same MMRIA field.
        3. If both FRACE22 and FRACE23 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value22) || string.IsNullOrWhiteSpace(value23)))
        {
            value = $"{value22}|{value23}";
        }
        else if (!string.IsNullOrWhiteSpace(value22))
        {
            value = $"{value22}";
        }
        else
        {
            value = $"{value23}";
        }

        return value;
    }

    public static string DOFP_MO_NAT_Rule(string value)
    {
        /*
        If DOFP_MO is in 01-12, transfer number verbatim to MMRIA field (bfdcppcdo1pv_month).

        If DOFP_MO = 99 --> bfdcppcdo1pv_month = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    public static string DOFP_DY_NAT_Rule(string value)
    {
        /*If DOFP_DY is in 01-31, transfer number verbatim to MMRIA field (bfdcppcdo1pv_day).

        If DOFP_DY = 99 --> bfdcppcdo1pv_day = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    public static string DOFP_YR_NAT_Rule(string value)
    {
        /*If DOFP_YR is not equal to 8888 or 9999, transfer number verbatim to MMRIA field (bfdcppcdo1pv_year).

        If DOFP_YR = 9999 --> bfdcppcdo1pv_year = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    public static string DOLP_MO_NAT_Rule(string value)
    {
        /*If DOLP_MO is in 01-12, transfer number verbatim to MMRIA field (bfdcppcdolpv_month).

        If DOLP_MO = 99 --> bfdcppcdolpv_month = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    public static string DOLP_DY_NAT_Rule(string value)
    {
        /*If DOLP_DY is in 01-31, transfer number verbatim to MMRIA field (bfdcppcdolpv_day).

        If DOLP_DY = 99 --> bfdcppcdolpv_day = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.*/
        if (value == "88" || value == "99")
            value = "9999";
        return value;
    }

    public static string DOLP_YR_NAT_Rule(string value)
    {
        /*If DOLP_YR is not equal to 8888 or 9999, transfer number verbatim to MMRIA field (bfdcppcdolpv_year).

        If DOLP_YR = 9999 --> bfdcppcdolpv_year = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    public static string CIGPN_NAT_Rule(string value)
    {
        /*If CIGPN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_p3_month. 
        2. bfdcpcs_p3m_type = 0 Cigarette(s). 

        If CIGPN = 99, then do:
        1. bfdcpcs_p3_month = (blank).
        2. bfdcpcs_p3m_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string CIGPN_Type_NAT_Rule(string value)
    {
        /*If CIGPN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_p3_month. 
        2. bfdcpcs_p3m_type = 0 Cigarette(s). 

        If CIGPN = 99, then do:
        1. bfdcpcs_p3_month = 9999 (blank).
        2. bfdcpcs_p3m_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    public static string CIGFN_NAT_Rule(string value)
    {
        /*If CIGFN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_1st. 
        2. bfdcpcs_t1_type = 0 Cigarette(s). 

        If CIGFN = 99, then do:
        1. bfdcpcs_t_1st = 9999 (blank).
        2. bfdcpcs_t1_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string CIGFN_Type_NAT_Rule(string value)
    {
        /*If CIGFN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_1st. 
        2. bfdcpcs_t1_type = 0 Cigarette(s). 

        If CIGFN = 99, then do:
        1. bfdcpcs_t_1st = 9999 (blank).
        2. bfdcpcs_t1_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    public static string CIGSN_NAT_Rule(string value)
    {
        /*If CIGSN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_2nd. 
        2. bfdcpcs_t2_type = 0 Cigarette(s). 

        If CIGSN = 99, then do:
        1. bfdcpcs_t_2nd = 9999 (blank).
        2. bfdcpcs_t2_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string CIGSN_Type_NAT_Rule(string value)
    {
        /*If CIGSN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_2nd. 
        2. bfdcpcs_t2_type = 0 Cigarette(s). 

        If CIGSN = 99, then do:
        1. bfdcpcs_t_2nd = 9999 (blank).
        2. bfdcpcs_t2_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    public static string CIGLN_NAT_Rule(string value)
    {
        /*If CIGLN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_3rd. 
        2. bfdcpcs_t3_type = 0 Cigarette(s). 

        If CIGLN = 99, then do:
        1. bfdcpcs_t_3rd = 9999 (blank).
        2. bfdcpcs_t3_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string CIGLN_Type_NAT_Rule(string value)
    {
        /*If CIGLN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_3rd. 
        2. bfdcpcs_t3_type = 0 Cigarette(s). 

        If CIGLN = 99, then do:
        1. bfdcpcs_t_3rd = 9999 (blank).
        2. bfdcpcs_t3_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    public static string CIG_none_or_not_specified_NAT_Rule(string value1, string value2, string value3, string value4)
    {
        /*
        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        string determinedValue = "9999";

        if 
        (
            value1 == "99" && 
            value2 == "99" && 
            value3 == "99" && 
            value4 == "99"
        )
        {
            determinedValue = "7777";
        }
        else if 
        (
            (
                value1 == "00" && 
                value2 == "00" && 
                value3 == "00" && 
                value4 == "00"
            ) || 
            (
                value1 == "0" && 
                value2 == "0" && 
                value3 == "0" && 
                value4 == "0"
            )
        )
        {
            determinedValue = "0";
        }
        return determinedValue;
    }

    public static string PDIAB_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PDIAB = Y --> bfdcprf_rfit_pregn = 0 Prepregnancy Diabetes

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */

        if (value == "Y")
            value = "0";

        return value;
    }
    public static string GDIAB_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        GDIAB = Y --> bfdcprf_rfit_pregn = 1 Gestational Diabetes

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */

        if (value == "Y")
            value = "1";

        return value;
    }
    public static string PHYPE_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PHYPE = Y --> bfdcprf_rfit_pregn = 2 Prepregnacy Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. 
        */
        if (value == "Y")
            value = "2";

        return value;
    }
    public static string GHYPE_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        GHYPE = Y --> bfdcprf_rfit_pregn = 3 Gestational Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "3";

        return value;
    }
    public static string PPB_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PPB = Y --> bfdcprf_rfit_pregn = 5 Previous Preterm Birth

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "5";

        return value;
    }
    public static string PPO_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PPO = Y --> bfdcprf_rfit_pregn = 6 Other Previous Poor Outcome

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "6";

        return value;
    }
    public static string INFT_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT = Y --> bfdcprf_rfit_pregn = 7 Pregnancy Resulted from Infertility Treatment

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "7";

        return value;
    }
    public static string PCES_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PCES = Y --> bfdcprf_rfit_pregn = 10 Mother had a Previous Cesarean Delivery

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "10";

        return value;
    }

    public static string GON_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        GON = Y --> bfdcp_ipotd_pregn = 2 Gonorrhea

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }
    public static string SYPH_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        SYPH = Y --> bfdcp_ipotd_pregn = 3 Syphilis

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }
    public static string HSV_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        HSV = Y --> bfdcp_ipotd_pregn = 11 Herpes Simplex [HSV]

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "11";

        return value;
    }
    public static string CHAM_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        CHAM = Y --> bfdcp_ipotd_pregn = 6 Chlamydia

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "6";

        return value;
    }
    public static string HEPB_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        HEPB = Y --> bfdcp_ipotd_pregn = 0 Hepatitis B (live birth only)

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }
    public static string HEPC_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] to populate MMRIA multi-select field bfdcp_ipotd_pregn). 

        HEPC = Y --> bfdcp_ipotd_pregn = 1 Hepatitis C (live birth only)

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 6 IJE fields [GON, SYPH, HSV, CHAM, HEPB, HEPC] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    public static string CERV_NAT_Rule(string value)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

        CERV = Y --> bfdcp_o_proce = 0 Cervical Cerclage

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }
    public static string TOC_NAT_Rule(string value)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

        TOC = Y --> bfdcp_o_proce = 1 Tocolysis

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    public static string ECVS_NAT_Rule(string value)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

        ECVS = Y --> bfdcp_o_proce = 2 External Cephalic Version: Successful

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    public static string ECVF_NAT_Rule(string value)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

        ECVS = Y --> bfdcp_o_proce = 3 External Cephalic Version: Failed

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

        If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }

    public static string PROM_NAT_Rule(string value)
    {
        /*Use values from 3 IJE fields [PROM, PRIC, PROL] to populate MMRIA multi-select field (bfdcp_oo_labor). 

        PROM = Y --> bfdcp_oo_labor = 0 Premature Rupture of Membranes (Prolonged)

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "N", then bfdcp_oo_labor = 3 None of the above

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "U" then bfdcp_oo_labor = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }

    public static string PRIC_NAT_Rule(string value)
    {
        /*Use values from 3 IJE fields [PROM, PRIC, PROL] to populate MMRIA multi-select field (bfdcp_oo_labor). 

        PRIC = Y --> bfdcp_oo_labor = 2 Precipitous labor (< 3 hours)

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "N", then bfdcp_oo_labor = 3 None of the above

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "U" then bfdcp_oo_labor = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    public static string PROL_NAT_Rule(string value)
    {
        /*Use values from 3 IJE fields [PROM, PRIC, PROL] to populate MMRIA multi-select field (bfdcp_oo_labor). 

        PROL = Y --> bfdcp_oo_labor = 1 Prolonged labor (> 20 hours)

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "N", then bfdcp_oo_labor = 3 None of the above

        If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "U" then bfdcp_oo_labor = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    public static string INDL_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        INDL = Y --> bfdcp_cola_deliv = 0 Induction of labor

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }

    public static string AUGL_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        AUGL = Y --> bfdcp_cola_deliv = 4 Augmentation of labor

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }

    public static string NVPR_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        NVPR = Y --> bfdcp_cola_deliv = 8 Non-vertex presentation

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "8";

        return value;
    }

    public static string STER_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        STER = Y --> bfdcp_cola_deliv = 1 Steroids (glucocorticoids) for fetal lung maturation received by mother prior to delivery

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    public static string ANTB_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        ANTB = Y --> bfdcp_cola_deliv = 5 Antibiotics received by the mother during labor

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "5";

        return value;
    }

    public static string CHOR_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        CHOR = Y --> bfdcp_cola_deliv = 2 Clinical chorioamnionitis diagnosed during labor or maternal temperature >= 38 degrees C (100.4 degrees F)

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    public static string MECS_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        MECS = Y --> bfdcp_cola_deliv = 6 Moderate to heavy meconium staining of the amniotic fluid

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "6";

        return value;
    }

    public static string FINT_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        FINT = Y --> bfdcp_cola_deliv = 7 Fetal intolerance of labor such that one or more of the following actions was taken: in-utero resuscitative measures, further fetal assessment, or operative delivery 

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "7";

        return value;
    }

    public static string ESAN_NAT_Rule(string value)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

        ESAN = Y --> bfdcp_cola_deliv = 3 Epidural or spinal anesthesia during labor

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

        If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }
    public static string TLAB_NAT_Rule(string value)
    {
        /*Y = Yes -> 1 Yes
        N = No -> 0 No
        U = Unknown -> 7777 Unknown
        X = Not Applicable -> 2 Not Applicable

        Map empty rows to 9999 (blank)
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            case "X":
                value = "2";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    public static string MTR_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        MTR = Y --> bfdcp_m_morbi = 0 Maternal transfusion

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }

    public static string PLAC_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        PLAC = Y --> bfdcp_m_morbi = 3 Third or fourth degree perineal laceration

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }

    public static string RUT_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        RUT = Y --> bfdcp_m_morbi = 5 Ruptured uterus

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "5";

        return value;
    }

    public static string UHYS_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        UHYS = Y --> bfdcp_m_morbi = 1 Unplanned hysterectomy

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    public static string AINT_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        AINT = Y --> bfdcp_m_morbi = 4 Admission to intensive care unit

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }

    public static string UOPR_NAT_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        UOPR = Y --> bfdcp_m_morbi = 2 Unplanned operating room procedure following delivery

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    public static string BWG_NAT_Rule(string value)
    {
        /*If BWG is in 0000-9998, do the following:
        1. Transfer number verbatim to bcifsbadbw_go_pound.
        2. Set value for bcifsbadbw_uo_measu to 0 Grams.

        If BWG = 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        */
        if (value == "9999")
            value = "";

        return value;
    }

    public static string BWG_measu_NAT_Rule(string value)
    {
        /*If BWG is in 0000-9998, do the following:
        1. Transfer number verbatim to bcifsbadbw_go_pound.
        2. Set value for bcifsbadbw_uo_measu to 0 Grams.

        If BWG = 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        */
        if (value == "9999")
            value = "9999";
        else
            value = "0";

        return value;
    }

    public static string PLUR_Custom_NAT_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "1";
                break;
            case "02":
            case "2":
                value = "2";
                break;
            case "03":
            case "3":
                value = "3";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = "4";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }
    public static string PLUR_sigt_NAT_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "";
                break;
            case "02":
            case "2":
                value = "";
                break;
            case "03":
            case "3":
                value = "";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = value;
                break;
            default:
                value = "";
                break;
        }

        return value;
    }
    public static string PLUR_gesta_NAT_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "0";
                break;
            case "02":
            case "2":
                value = "1";
                break;
            case "03":
            case "3":
                value = "1";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = "1";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string AVEN1_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        AVEN1 = Y --> bcifs_aco_newbo = 0 Assisted ventilation required immediately following delivery

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }

    public static string AVEN6_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        AVEN6 = Y --> bcifs_aco_newbo = 3 Assisted ventilation required for more than 6 hours

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }

    public static string NICU_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        NICU = Y --> bcifs_aco_newbo = 4 NICU admission

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }

    public static string SURF_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        SURF = Y --> bcifs_aco_newbo = 1 Newborn given surfactant replacement therapy

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    public static string ANTI_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        ANTI = Y --> bcifs_aco_newbo = 5 Antibiotics received by the newborn for suspected neonatal sepsis

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "5";

        return value;
    }

    public static string SEIZ_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        SEIZ = Y --> bcifs_aco_newbo = 2 Seizure or serious neurologic dysfunction

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    public static string BINJ_NAT_Rule(string value)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        BINJ = Y --> bcifs_aco_newbo = 6 Significant birth injury (skeletal fracture(s), peripheral nerve injury and or soft tissue or solid organ hemorrhage which requires intervention)

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        if (value == "Y")
            value = "6";

        return value;
    }

    public static string ANEN_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        ANEN = Y --> bcifs_c_anoma = 0 Anencephaly

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }

    public static string MNSB_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        MNSB = Y --> bcifs_c_anoma = 9 Meningomyelocele or Spina bifida

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "9";

        return value;
    }

    public static string CCHD_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CCHD = Y --> bcifs_c_anoma = 1 Cyanotic congenital heart disease

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }

    public static string CDH_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CDH = Y --> bcifs_c_anoma = 10 Congenital diaphragmatic hernia

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "10";

        return value;
    }

    public static string OMPH_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        OMPH = Y --> bcifs_c_anoma = 2 Omphalocele

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }

    public static string GAST_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        GAST = Y --> bcifs_c_anoma = 11 Gastroschisis

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "11";

        return value;
    }

    public static string LIMB_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        LIMB = Y --> bcifs_c_anoma = 3 Limb reduction defect (excluding congenital amputation and dwarfing syndromes)

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }

    public static string CL_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CL = Y --> bcifs_c_anoma = 4 Cleft Lip with or without Cleft Palate

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }

    public static string CP_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CP = Y --> bcifs_c_anoma = 12 Cleft palate alone

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "12";

        return value;
    }

    public static string DOWT_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If DOWT = C --> bcifs_c_anoma = 6 Karyotype confirmed - Downs Syndrome
        If DOWT = P --> bcifs_c_anoma = 7 Karyotype pending - Downs Syndrome

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "C")
            value = "6";
        else if (value == "P")
            value = "7";

        return value;
    }

    public static string CDIT_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If CDIT = C --> bcifs_c_anoma = 14 Karyotype confirmed - Suspected chromosomal disorder
        If CDIT = P --> bcifs_c_anoma = 15 Karyotype pending - Suspected chromosomal disorder

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "C")
            value = "14";
        else if (value == "P")
            value = "15";

        return value;
    }

    public static string HYPO_NAT_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        HYPO = Y --> bcifs_c_anoma = 8 Hypospadias

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "8";

        return value;
    }

    public static string MAGER_NAT_Rule(string value, string dob_YR, string dob_MO, string dob_day, string dodeliv_YR, string dodeliv_MO, string dodeliv_day)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the MMRIA value empty/blank*/
        if (value == "99")
            value = age_delivery(dob_YR, dob_MO, dob_day, dodeliv_YR, dodeliv_MO, dodeliv_day);

        return value;
    }

    public static string FAGER_NAT_Rule(string value, string dob_YR, string dob_MO, string dodeliv_YR, string dodeliv_MO, string dodeliv_day)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the MMRIA value empty/blank*/
        if (value == "99")
            value = age_delivery(dob_YR, dob_MO, "1", dodeliv_YR, dodeliv_MO, dodeliv_day);

        return value;
    }

    public static string EHYPE_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        EHYPE = Y --> bfdcprf_rfit_pregn = 4 Eclampsia Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "4";

        return value;
    }

    public static string INFT_DRG_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT_DRG = Y --> bfdcprf_rfit_pregn = 8 Fertility Enhancing Drugs, Artificial Insemination or Intrauterine Insemination

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "8";

        return value;
    }

    public static string INFT_ART_NAT_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT_ART = Y --> bfdcprf_rfit_pregn = 9 Assisted Reproductive Technology (e.g. in vitro fertilization (IVF), gamete intrafallopian transfer (GIFT))

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "9";

        return value;
    }


    public static string FBPLACD_ST_TER_C_NAT_Rule(string value)
    {
        /*Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)

        Map all other values to MMRIA field state listing*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }

    public static string FBPLACE_CNT_C_NAT_Rule(string value)
    {
        /*Map to MMRIA field country listing

        Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }


    public static object FET_maternal_morbidity_Rule(string value1, string value2, string value3, string value4, string value5, string value6)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        MTR = Y --> bfdcp_m_morbi = 0 Maternal transfusion

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N")
        //    determinedValues.Add("6");
        //else
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

        }

        return determinedValues.ToArray();
    }

    public static object FET_characteristics_of_labor_and_delivery_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7, string value8, string value9)
    {
        /*Use values from 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] to populate MMRIA multi-select field (bfdcp_cola_deliv). 

INDL = Y --> bfdcp_cola_deliv = 0 Induction of labor

If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "N", then bfdcp_cola_deliv = 9 None of the above

If every one of the 9 IJE fields [INDL, AUGL, NVPR, STER, ANTB, CHOR, MECS, FINT, ESAN] is equal to "U" then bfdcp_cola_deliv = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //     && value9 == "N")
        //    determinedValues.Add("9");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
                && value9 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);
        }

        return determinedValues.ToArray();
    }

    public static object FET_onset_of_labor_Rule(string value1, string value2, string value3)
    {
        /*Use values from 3 IJE fields [PROM, PRIC, PROL] to populate MMRIA multi-select field (bfdcp_oo_labor). 

PROM = Y --> bfdcp_oo_labor = 0 Premature Rupture of Membranes (Prolonged)

If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "N", then bfdcp_oo_labor = 3 None of the above

If every one of the 3 IJE fields [PROM, PRIC, PROL] is equal to "U" then bfdcp_oo_labor = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N")
        //    determinedValues.Add("3");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

        }

        return determinedValues.ToArray();
    }

    public static object FET_obstetric_procedures_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values from 4 IJE fields [CERV, TOC, ECVS, ECVF] to populate MMRIA multi-select field (bfdcp_o_proce). 

CERV = Y --> bfdcp_o_proce = 0 Cervical Cerclage

If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "N", then bfdcp_o_proce = 4 None of the above

If every one of the 4 IJE fields [CERV, TOC, ECVS, ECVF] is equal to "U" then bfdcp_o_proce = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N")
        //    determinedValues.Add("4");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

        }

        return determinedValues.ToArray();
    }


    public static object FET_infections_present_or_treated_during_pregnancy_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7, string value8, string value9, string value10, string value11, string value12)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        GON = Y --> bfdcp_ipotd_pregn = 2 Gonorrhea

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //     && value9 == "N" && value10 == "N" && value11 == "N" && value12 == "N")
        //    determinedValues.Add("17");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
                && value9 == "U" && value10 == "U" && value11 == "U" && value12 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);

            if (int.TryParse(value10, out result))
                determinedValues.Add(value10);

            if (int.TryParse(value11, out result))
                determinedValues.Add(value11);

            if (int.TryParse(value12, out result))
                determinedValues.Add(value12);
        }

        return determinedValues.ToArray();
    }

    public static object FET_risk_factors_in_this_pregnancy_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7, string value8, string value9)
    {
        //    /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        //   EHYPE = Y --> bfdcprf_rfit_pregn = 4 Eclampsia Hypertension

        //   If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        //   If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        //   *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */

        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //    && value9 == "N")
        //    determinedValues.Add("11");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
            && value9 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

        }

        return determinedValues.ToArray();
    }

    public static object FET_congenital_Rule(string value1, string value2, string value3, string value4, string value5
        , string value6, string value7, string value8, string value9
        , string value10, string value11, string value12)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/

        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N" && value8 == "N"
        //     && value9 == "N" && value10 == "N" && value11 == "N" && value12 == "N")
        //    determinedValues.Add("17");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U" && value8 == "U"
                && value9 == "U" && value10 == "U" && value11 == "U" && value12 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);

            if (int.TryParse(value8, out result))
                determinedValues.Add(value8);

            if (int.TryParse(value9, out result))
                determinedValues.Add(value9);

            if (int.TryParse(value10, out result))
                determinedValues.Add(value10);

            if (int.TryParse(value11, out result))
                determinedValues.Add(value11);

            if (int.TryParse(value12, out result))
                determinedValues.Add(value12);
        }

        return determinedValues.ToArray();
    }

    public static object FET_abnormal_Rule(string value1, string value2, string value3, string value4, string value5, string value6, string value7)
    {
        /*Use values from 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] to populate MMRIA multi-select field (bcifs_aco_newbo). 

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "N", then bcifs_aco_newbo = 8 None of the above

        If every one of the 7 IJE fields [AVEN1, AVEN6, NICU, SURF, ANTI, SEIZ, BINJ] is equal to "U" then bcifs_aco_newbo = 7777 Unknown*/
        List<string> determinedValues = new List<string>();

        //if (value1 == "N" && value2 == "N" && value3 == "N" && value4 == "N"
        //    && value5 == "N" && value6 == "N" && value7 == "N")
        //    determinedValues.Add("8");
        //else 
        if (value1 == "U" && value2 == "U" && value3 == "U" && value4 == "U"
            && value5 == "U" && value6 == "U" && value7 == "U")
            determinedValues.Add("7777");
        else
        {
            if (int.TryParse(value1, out int result))
                determinedValues.Add(value1);

            if (int.TryParse(value2, out result))
                determinedValues.Add(value2);

            if (int.TryParse(value3, out result))
                determinedValues.Add(value3);

            if (int.TryParse(value4, out result))
                determinedValues.Add(value4);

            if (int.TryParse(value5, out result))
                determinedValues.Add(value5);

            if (int.TryParse(value6, out result))
                determinedValues.Add(value6);

            if (int.TryParse(value7, out result))
                determinedValues.Add(value7);
        }

        return determinedValues.ToArray();
    }

    public static string FET_LOCATION_OF_RESIDENCE_street_Rule(string stnum_r, string predir_r, string stname_r, string stdesig_r, string postdir_r)
    {
        //Map to MMRIA field via Merge with other place of death street fields(STNUM_D, PREDIR_D, STNAME_D, STDESIG_D, POSTDIR_D) 1 of 5
        string determinedValue = $"{stnum_r} {predir_r} {stname_r} {stdesig_r} {postdir_r}";

        return determinedValue;
    }

    public static string FET_DATE_OF_DELIVERY_Rule(string year, string month, string day)
    {
        //2.Merge 3 fields(IDOB_MO, IDOB_DY, IDOB_YR) map resulting date to MMRIA field -date_of _delivery(bcifsri_do_deliv)."
        string determinedValue = $"{year}-{month}-{day}";

        return determinedValue;
    }


    public static string MDOB_YR_FET_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/
        if (value == "9999")
            value = "9999";

        return value;
    }

    public static string MDOB_MO_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.  

        If value = 99, map to 9999 (blank). */

        if (value == "99")
            value = "9999";

        return value;
    }

    public static string MDOB_DY_FET_Rule(string value)
    {
        /*If value is in 01-31, transfer number verbatim to MMRIA field.  
            * If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    public static string FDOB_YR_FET_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/

        if (value == "9999")
            value = "9999";

        return value;
    }

    public static string FDOB_MO_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    public static string MARN_FET_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    public static string MEDUC_FET_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = 8th Grade or Less
        2  -> 1 = 9th-12th Grade; No Diploma
        3  -> 2 = High School Grad or GED Completed 
        4  -> 3 = Some college, but no degree
        5  -> 4 = Associate Degree
        6  -> 5 = Bachelor's Degree
        7  -> 6 = Master's Degree
        8  -> 7 = Doctorate or Professional Degree
        9  -> 7777 = Unknown*/

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "6":
                value = "5";
                break;
            case "7":
                value = "6";
                break;
            case "8":
                value = "7";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    public static string ATTEND_FET_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = MD
        2 -> 1 = DO
        3 -> 2 = CNM/CM
        4 -> 3 = Other Midwife
        5 -> 4 = Other 
        9 -> 7777 = Unknown*/

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string TRAN_FET_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    public static string NPREV_FET_Rule(string value)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field. 

        If value = 99, map to 9999 (blank)*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string HFT_FET_Rule(string value)
    {
        /*If value is in 1-8, transfer number verbatim to MMRIA field. 

        If value = 9, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "9")
            value = "";

        return value;
    }

    public static string HIN_FET_Rule(string value)
    {
        /*If value is in 00-11, transfer number verbatim to MMRIA field. 

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string PWGT_FET_Rule(string value)
    {
        /*If value is in 050-400, transfer number verbatim to MMRIA field.

        If value = 999, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "999" || value == "9999")
            value = "";

        return value;
    }

    public static string DWGT_FET_Rule(string value)
    {
        /*If value is in 050-450, transfer number verbatim to MMRIA field.  

        If value = 999, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "999" || value == "9999")
            value = "";

        return value;
    }

    public static string WIC_FET_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    public static string PLBL_FET_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.  

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string PLBD_FET_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.  

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99")
            value = "";

        return value;
    }

    public static string POPO_FET_Rule(string value)
    {
        /*If value is in 00-30, transfer number verbatim to MMRIA field.

        If value = 99, map to MMRIA value for missing [looks like this is just leaving the value empty/blank]*/

        if (value == "99" || value == "9999")
            value = "";

        return value;
    }

    public static string MLLB_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 88 or 99, map to 9999 (blank).*/

        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    public static string YLLB_FET_Rule(string value)
    {
        /*If value is not 8888 or 9999, transfer number verbatim to MMRIA field.

        If value = 8888 or 9999, map to 9999 (blank).*/

        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    public static string MOPO_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 88 or 99, map to 9999 (blank).*/

        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    public static string YOPO_FET_Rule(string value)
    {
        /*If value is not 8888 or 9999, transfer number verbatim to MMRIA field.  

        If value = 8888 or 9999, map to 9999 (blank).*/

        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    public static string DLMP_YR_FET_Rule(string value)
    {
        /*If value is not 9999, transfer number verbatim to MMRIA field.

        If value = 9999, map to 9999 (blank).*/

        if (value == "9999")
            value = "9999";

        return value;
    }

    public static string DLMP_MO_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/
        if (value == "99")
            value = "9999";

        return value;
    }

    public static string DLMP_DY_FET_Rule(string value)
    {
        /*If value is in 01-31, transfer number verbatim to MMRIA field.

        If value = 99, map to 9999 (blank).*/

        if (value == "99")
            value = "9999";

        return value;
    }

    public static string NPCES_FET_Rule(string value)
    {
        /*Transfer number verbatim to MMRIA field.  Map 99 to 9999 (blank)*/

        if (value == "99")
            value = "9999";

        return value;
    }

    public static string ATTF_FET_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  ->  7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    public static string ATTV_FET_Rule(string value)
    {
        /*Map character to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        Y  -> 1 =Yes
        N  -> 0 = No
        U  -> 7777 = Unknown
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    public static string PRES_FET_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = Cephalic
        2 -> 1 = Breech
        3 -> 4 = Other
        9 -> 7777 = Unknown*/

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "4";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string ROUT_FET_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = Vaginal/Spontaneous
        2 -> 1 = Vaginal/Forceps
        3  -> 2 = Vaginal/Vacuum
        4  -> 3 = Cesarean
        9  -> 7777 = Unknown*/

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string OWGEST_FET_Rule(string value)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the value empty/blank. */

        if (value == "99")
            value = "";

        return value;
    }

    public static string SORD_FET_Rule(string value)
    {
        /*If value is in 01-12, transfer number verbatim to MMRIA field.  

        If value = 99, leave the MMRIA value empty/blank.*/

        if (value == "99")
            value = "";

        return value;
    }


    public static string FEDUC_FET_Rule(string value)
    {
        /*Map number to MMRIA code values as follows:
        Blank fields -> 9999 (blank)
        1 -> 0 = 8th Grade or Less
        2  -> 1 = 9th-12th Grade; No Diploma
        3  -> 2 = High School Grad or GED Completed 
        4  -> 3 = Some college, but no degree
        5  -> 4 = Associate Degree
        6  -> 5 = Bachelor's Degree
        7  -> 6 = Master's Degree
        8  -> 7 = Doctorate or Professional Degree
        9  -> 7777 = Unknown*/

        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "3";
                break;
            case "5":
                value = "4";
                break;
            case "6":
                value = "5";
                break;
            case "7":
                value = "6";
                break;
            case "8":
                value = "7";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    public static string FSEX_FET_Rule(string value)
    {
        /*M = Male -> 0 Male
        F = Female -> 1 Female
        U = Unknown -> 7777 Unknown

        Map empty rows to 9999 (blank)
        */
        switch (value?.ToUpper())
        {
            case "M":
                value = "0";
                break;
            case "F":
                value = "1";
                break;
            case "U":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }
    public static string DPLACE_Custom_FET_Rule(string value)
    {
        /*1 = Hospital -> bfdcpfodd_to_place = 0 Hospital & bfdcpfodd_whd_plann = 9999 (blank)

        2 = Freestanding Birth Center -> bfdcpfodd_to_place = 1 Free Standing Birth Center & bfdcpfodd_whd_plann = 9999 (blank)

        3 = Home (Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 1 Yes

        4 = Home (Not Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 0 No

        5 = Home (Unknown if Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 7777 Unknown

        6 = Clinic/Doctor's Office -> bfdcpfodd_to_place = 3 Clinic/Doctor's office & bfdcpfodd_whd_plann = 9999 (blank)

        7 = Other -> bfdcpfodd_to_place = 4 Other & bfdcpfodd_whd_plann = 9999 (blank)

        9 = Unknown --> bfdcpfodd_to_place = 7777 Unknown & bfdcpfodd_whd_plann = 9999 (blank)*/
        switch (value?.ToUpper())
        {
            case "1":
                value = "0";
                break;
            case "2":
                value = "1";
                break;
            case "3":
                value = "2";
                break;
            case "4":
                value = "2";
                break;
            case "5":
                value = "2";
                break;
            case "6":
                value = "3";
                break;
            case "7":
                value = "4";
                break;
            case "9":
                value = "7777";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }
    public static string DPLACE_plann_Rule(string value)
    {
        /*1 = Hospital -> bfdcpfodd_to_place = 0 Hospital & bfdcpfodd_whd_plann = 9999 (blank)

            2 = Freestanding Birth Center -> bfdcpfodd_to_place = 1 Free Standing Birth Center & bfdcpfodd_whd_plann = 9999 (blank)

            3 = Home (Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 1 Yes

            4 = Home (Not Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 0 No

            5 = Home (Unknown if Intended) -> bfdcpfodd_to_place = 2 Home Birth & bfdcpfodd_whd_plann = 7777 Unknown

            6 = Clinic/Doctor's Office -> bfdcpfodd_to_place = 3 Clinic/Doctor's office & bfdcpfodd_whd_plann = 9999 (blank)

            7 = Other -> bfdcpfodd_to_place = 4 Other & bfdcpfodd_whd_plann = 9999 (blank)

            9 = Unknown --> bfdcpfodd_to_place = 7777 Unknown & bfdcpfodd_whd_plann = 9999 (blank)*/
        switch (value?.ToUpper())
        {
            case "1":
                value = "9999";
                break;
            case "2":
                value = "9999";
                break;
            case "3":
                value = "1";
                break;
            case "4":
                value = "0";
                break;
            case "5":
                value = "7777";
                break;
            case "6":
                value = "9999";
                break;
            case "7":
                value = "9999";
                break;
            case "9":
                value = "9999";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }

    public static string BPLACEC_ST_TER_FET_Rule(string value)
    {
        /*Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)

        Map all other values to MMRIA field state listing*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }
    public static string BPLACEC_CNT_FET_Rule(string value)
    {
        /*Map to MMRIA field country listing 

        Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }
    
    public static string METHNIC_FET_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values of METHNIC1, METHNIC2, METHNIC3, METHNIC4 to populate MMRIA field bfdcpdom_ioh_origi.

        H --> bfdcpdom_ioh_origi = 1 Yes, Mexican, Mexican American, Chicano
        H --> bfdcpdom_ioh_origi = 2 Yes, Puerto Rican
        H --> bfdcpdom_ioh_origi = 3 Yes, Cuban
        H --> bfdcpdom_ioh_origi = 4 Yes, Other Spanish/Hispanic/Latino


        If METHNIC1 = N and METHNIC2 = N and METHNIC3 = N and METHNIC 4 = N --> bfdcpdom_ioh_origi = 0 No, Not Spanish/Hispanic/Latino

        If METHNIC1 = U and METHNIC2 = U and METHNIC3 = U and METHNIC4 = U --> bfdcpdom_ioh_origi = 7777 Unknown

        If METHNIC1 = (empty) and METHNIC2 = (empty) and METHNIC3 = (empty) and METHNIC4 = (empty) --> bfdcpdom_ioh_origi = 9999 (blank)*/
        string determinedValue;

        if (value1?.ToUpper() == "H")
        {
            determinedValue = "1";
        }
        else if (value2?.ToUpper() == "H")
        {
            determinedValue = "2";
        }
        else if (value3?.ToUpper() == "H")
        {
            determinedValue = "3";
        }
        else if (value4?.ToUpper() == "H")
        {
            determinedValue = "4";
        }
        else if (value1?.ToUpper() == "N" && value2?.ToUpper() == "N" && value3?.ToUpper() == "N" && value4?.ToUpper() == "N")
        {
            determinedValue = "0";
        }
        else if (value1?.ToUpper() == "U" && value2?.ToUpper() == "U" && value3?.ToUpper() == "U" && value4?.ToUpper() == "U")
        {
            determinedValue = "7777";
        }
        else
        {
            determinedValue = "9999";
        }

        return determinedValue;
    }

    public static string[] MRACE_FET_Rule(string value1, string value2, string value3, string value4, string value5,
        string value6, string value7, string value8, string value9, string value10,
        string value11, string value12, string value13, string value14, string value15)
    {
        /*Use values from MRACE1 through MRACE15 to populate MMRIA multi-select field (bfdcpr_ro_mothe).

        MRACE1 = Y --> bfdcpr_ro_mothe = 0 White
        MRACE2 = Y --> bfdcpr_ro_mothe = 1 Black or African American
        MRACE3 = Y --> bfdcpr_ro_mothe = 2 American Indian or Alaska Native
        MRACE4 = Y --> bfdcpr_ro_mothe = 7 Asian Indian
        MRACE5 = Y --> bfdcpr_ro_mothe = 8 Chinese
        MRACE6 = Y --> bfdcpr_ro_mothe = 9 Filipino
        MRACE7 = Y --> bfdcpr_ro_mothe = 10 Japanese
        MRACE8 = Y --> bfdcpr_ro_mothe = 11 Korean
        MRACE9 = Y --> bfdcpr_ro_mothe = 12 Vietnamese
        MRACE10 = Y --> bfdcpr_ro_mothe = 13 Other Asian
        MRACE11 = Y --> bfdcpr_ro_mothe = 3 Native Hawaiian
        MRACE12 = Y --> bfdcpr_ro_mothe = 4 Guamanian or Chamorro
        MRACE13 = Y --> bfdcpr_ro_mothe = 5 Samoan
        MRACE14 = Y --> bfdcpr_ro_mothe = 6 Other Pacific Islander
        MRACE15 = Y --> bfdcpr_ro_mothe = 14 Other Race

        If every one of MRACE1 through MRACE15 is equal to "N", then bfdcpr_ro_mothe = 8888 (Race Not Specified)*/

        List<string> determinedValues = new List<string>();

        if (value1?.ToUpper() == "Y")
        {
            determinedValues.Add("0");
        }
        if (value2?.ToUpper() == "Y")
        {
            determinedValues.Add("1");
        }
        if (value3?.ToUpper() == "Y")
        {
            determinedValues.Add("2");
        }
        if (value4?.ToUpper() == "Y")
        {
            determinedValues.Add("7");
        }
        if (value5?.ToUpper() == "Y")
        {
            determinedValues.Add("8");
        }
        if (value6?.ToUpper() == "Y")
        {
            determinedValues.Add("9");
        }
        if (value7?.ToUpper() == "Y")
        {
            determinedValues.Add("10");
        }
        if (value8?.ToUpper() == "Y")
        {
            determinedValues.Add("11");
        }
        if (value9?.ToUpper() == "Y")
        {
            determinedValues.Add("12");
        }
        if (value10?.ToUpper() == "Y")
        {
            determinedValues.Add("13");
        }
        if (value11?.ToUpper() == "Y")
        {
            determinedValues.Add("3");
        }
        if (value12?.ToUpper() == "Y")
        {
            determinedValues.Add("4");
        }
        if (value13?.ToUpper() == "Y")
        {
            determinedValues.Add("5");
        }
        if (value14?.ToUpper() == "Y")
        {
            determinedValues.Add("6");
        }
        if (value15?.ToUpper() == "Y")
        {
            determinedValues.Add("14");
        }
        if (determinedValues.Count == 0)
        {
            determinedValues.Add("8888");
        }
        return determinedValues.ToArray();
    }

    public static string MRACE16_17_FET_Rule(string value16, string value17)
    {
        /*Combine MRACE16 and MRACE17 into one field (bfdcpr_p_tribe), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE16 to MMRIA field.
        2. Transfer string verbatim from MRACE17 and add to same MMRIA field.
        3. If both MRACE16 and MRACE17 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value16) && 
            !string.IsNullOrWhiteSpace(value17)
        )
        {
            value = $"{value16}, {value17}";
        }
        else if (!string.IsNullOrWhiteSpace(value16))
        {
            value = $"{value16}";
        }
        else if (!string.IsNullOrWhiteSpace(value17))
        {
            value = $"{value17}";
        }

        return value;
    }
    
    public static string MRACE18_19_FET_Rule(string value18, string value19)
    {
        /*Combine MRACE18 and MRACE19 into one field (bfdcpr_o_asian), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE18 to MMRIA field.
        2. Transfer string verbatim from MRACE19 and add to same MMRIA field.
        3. If both MRACE18 and MRACE19 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value18) && 
            !string.IsNullOrWhiteSpace(value19)
        )
        {
            value = $"{value18}, {value19}";
        }
        else if (!string.IsNullOrWhiteSpace(value18))
        {
            value = $"{value18}";
        }
        else if (!string.IsNullOrWhiteSpace(value19))
        {
            value = $"{value19}";
        }

        return value;
       
    }
    
    public static string MRACE20_21_FET_Rule(string value20, string value21)
    {
        /*Combine MRACE20 and MRACE21 into one field (bfdcpr_op_islan), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE20 to MMRIA field.
        2. Transfer string verbatim from MRACE21 and add to same MMRIA field.
        3. If both MRACE20 and MRACE21 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value20) && 
            !string.IsNullOrWhiteSpace(value21)
        )
        {
            value = $"{value20}, {value21}";
        }
        else if (!string.IsNullOrWhiteSpace(value20))
        {
            value = $"{value20}";
        }
        else if(!string.IsNullOrWhiteSpace(value21))
        {
            value = $"{value21}";
        }

        return value;
    }
    
    public static string MRACE22_23_FET_Rule(string value22, string value23)
    {
        /*Combine MRACE22 and MRACE23 into one field (bfdcpr_o_race), separated by pipe delimiter. 

        1. Transfer string verbatim from MRACE22 to MMRIA field.
        2. Transfer string verbatim from MRACE23 and add to same MMRIA field.
        3. If both MRACE22 and MRACE23 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if 
        (
            !string.IsNullOrWhiteSpace(value22) &&
            !string.IsNullOrWhiteSpace(value23)
        )
        {
            value = $"{value22}, {value23}";
        }
        else if (!string.IsNullOrWhiteSpace(value22))
        {
            value = $"{value22}";
        }
        else if (!string.IsNullOrWhiteSpace(value23))
        {
            value = $"{value23}";
        }

        return value;
    }
    
    public static string DOFP_MO_FET_Rule(string value)
    {
        /*
        If DOFP_MO is in 01-12, transfer number verbatim to MMRIA field (bfdcppcdo1pv_month).

        If DOFP_MO = 99 --> bfdcppcdo1pv_month = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value; 
    }

    public static string DOFP_DY_FET_Rule(string value)
    {
        /*If DOFP_DY is in 01-31, transfer number verbatim to MMRIA field (bfdcppcdo1pv_day).

        If DOFP_DY = 99 --> bfdcppcdo1pv_day = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    public static string DOFP_YR_FET_Rule(string value)
    {
        /*If DOFP_YR is not equal to 8888 or 9999, transfer number verbatim to MMRIA field (bfdcppcdo1pv_year).

        If DOFP_YR = 9999 --> bfdcppcdo1pv_year = 9999 (blank).

        If DOFP_MO = 88 and DOFP_DY = 88 and DOFP_YR = 8888, then do the following:
        1. bfdcppcdo1pv_month = 9999 (blank) 
        2. bfdcppcdo1pv_day = 9999 (blank)
        3. bfdcppcdo1pv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    public static string DOLP_MO_FET_Rule(string value)
    {
        /*If DOLP_MO is in 01-12, transfer number verbatim to MMRIA field (bfdcppcdolpv_month).

        If DOLP_MO = 99 --> bfdcppcdolpv_month = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value; 
    }

    public static string DOLP_DY_FET_Rule(string value)
    {
        /*If DOLP_DY is in 01-31, transfer number verbatim to MMRIA field (bfdcppcdolpv_day).

        If DOLP_DY = 99 --> bfdcppcdolpv_day = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.*/
        if (value == "88" || value == "99")
            value = "9999";

        return value;
    }

    public static string DOLP_YR_FET_Rule(string value)
    {
        /*If DOLP_YR is not equal to 8888 or 9999, transfer number verbatim to MMRIA field (bfdcppcdolpv_year).

        If DOLP_YR = 9999 --> bfdcppcdolpv_year = 9999 (blank).

        If DOLP_MO = 88 and DOLP_DY = 88 and DOLP_YR = 8888, then do the following:
        1. bfdcppcdolpv_month = 9999 (blank)
        2. bfdcppcdolpv_day = 9999 (blank)
        3. bfdcppcdolpv_year = 9999 (blank)
        4. bfdcppc_to1pc_visit = 0 No Prenatal Care.

        No other values are populated for bfdcppc_to1pc_visit from IJE fields.*/
        if (value == "8888" || value == "9999")
            value = "9999";

        return value;
    }

    public static string CIGPN_Custom_FET_Rule(string value)
    {
        /*If CIGPN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_p3_month. 
        2. bfdcpcs_p3m_type = 0 Cigarette(s). 

        If CIGPN = 99, then do:
        1. bfdcpcs_p3_month =  (blank).
        2. bfdcpcs_p3m_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "";

        return value;
    }
    
    public static string CIGPN_Type_FET_Rule(string value)
    {
        /*If CIGPN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_p3_month. 
        2. bfdcpcs_p3m_type = 0 Cigarette(s). 

        If CIGPN = 99, then do:
        1. bfdcpcs_p3_month = 9999 (blank).
        2. bfdcpcs_p3m_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/

        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    public static string CIGFN_Custom_FET_Rule(string value)
    {
        /*If CIGFN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_1st. 
        2. bfdcpcs_t1_type = 0 Cigarette(s). 

        If CIGFN = 99, then do:
        1. bfdcpcs_t_1st = 9999 (blank).
        2. bfdcpcs_t1_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "";

        return value;
    }
    public static string CIGFN_Type_FET_Rule(string value)
    {
        /*If CIGFN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_1st. 
        2. bfdcpcs_t1_type = 0 Cigarette(s). 

        If CIGFN = 99, then do:
        1. bfdcpcs_t_1st = 9999 (blank).
        2. bfdcpcs_t1_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }

    public static string CIGSN_Type_FET_Rule(string value)
    {
        /*If CIGSN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_2nd. 
        2. bfdcpcs_t2_type = 0 Cigarette(s). 

        If CIGSN = 99, then do:
        1. bfdcpcs_t_2nd = 9999 (blank).
        2. bfdcpcs_t2_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }
    public static string CIGSN_Custom_FET_Rule(string value)
    {
        /*If CIGSN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_2nd. 
        2. bfdcpcs_t2_type = 0 Cigarette(s). 

        If CIGSN = 99, then do:
        1. bfdcpcs_t_2nd = 9999 (blank).
        2. bfdcpcs_t2_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "";

        return value;
    }

    public static string CIGLN_Type_FET_Rule(string value)
    {
        /*If CIGLN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_3rd. 
        2. bfdcpcs_t3_type = 0 Cigarette(s). 

        If CIGLN = 99, then do:
        1. bfdcpcs_t_3rd = 9999 (blank).
        2. bfdcpcs_t3_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "9999";
        else
            value = "0";

        return value;
    }
    public static string CIGLN_Custom_FET_Rule(string value)
    {
        /*If CIGLN value in 00-98, then do:
        1. Transfer number verbatim to MMRIA field bfdcpcs_t_3rd. 
        2. bfdcpcs_t3_type = 0 Cigarette(s). 

        If CIGLN = 99, then do:
        1. bfdcpcs_t_3rd = 9999 (blank).
        2. bfdcpcs_t3_type = 9999 (blank) 

        Also look across 4 IJE fields (CIGPN, CIGFN, CIGSN, CIGLN) to fill out MMRIA field bfdcpcs_non_speci:
        1. If CIGPN = 99 and CIGFN = 99 and CIGSN = 99 and CIGLN = 99, then bfdcpcs_non_speci = 7777 Unknown.
        2. If CIGPN = 00 and CIGFN = 00 and CIGSN = 00 and CIGLN = 00 then bfdcpcs_non_speci = 0 None.
        3. Otherwise leave bfdcpcs_non_speci as 9999 (blank).*/
        if (value == "99")
            value = "";

        return value;
    }

    public static string PDIAB_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PDIAB = Y --> bfdcprf_rfit_pregn = 0 Prepregnancy Diabetes

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. 
        */
        if (value == "Y")
            value = "0";

        return value;
    }
    public static string GDIAB_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        GDIAB = Y --> bfdcprf_rfit_pregn = 1 Gestational Diabetes

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "1";

        return value;
    }
    public static string PHYPE_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PHYPE = Y --> bfdcprf_rfit_pregn = 2 Prepregnacy Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. 
        */
        if (value == "Y")
            value = "2";

        return value;
    }
    public static string GHYPE_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        GHYPE = Y --> bfdcprf_rfit_pregn = 3 Gestational Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "3";

        return value;
    }
    public static string PPB_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PPB = Y --> bfdcprf_rfit_pregn = 5 Previous Preterm Birth

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "5";

        return value;
    }
    public static string PPO_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PPO = Y --> bfdcprf_rfit_pregn = 6 Other Previous Poor Outcome

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "6";

        return value;
    }
    public static string INFT_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT = Y --> bfdcprf_rfit_pregn = 7 Pregnancy Resulted from Infertility Treatment

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "7";

        return value;
    }
    public static string PCES_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        PCES = Y --> bfdcprf_rfit_pregn = 10 Mother had a Previous Cesarean Delivery

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. 
        */
        if (value == "Y")
            value = "10";

        return value;
    }
    public static string GON_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        GON = Y --> bfdcp_ipotd_pregn = 2 Gonorrhea

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }
    public static string SYPH_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        SYPH = Y --> bfdcp_ipotd_pregn = 3 Syphilis

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }
    public static string HSV_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        HSV = Y --> bfdcp_ipotd_pregn = 11 Herpes Simplex [HSV]

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "11";

        return value;
    }
    public static string CHAM_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        CHAM = Y --> bfdcp_ipotd_pregn = 6 Chlamydia

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "6";

        return value;
    }
    public static string LM_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        LM = Y --> bfdcp_ipotd_pregn = 4 Listeria

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }
    public static string GBS_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        GBS = Y --> bfdcp_ipotd_pregn = 8 Group B Streptococcus (fetal death(s) only)

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "8";

        return value;
    }
    public static string CMV_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        CMV = Y --> bfdcp_ipotd_pregn = 5 Cytomegalovirus

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "5";

        return value;
    }
    public static string B19_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        B19 = Y --> bfdcp_ipotd_pregn = 7 Parvovirus

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "7";

        return value;
    }
    public static string TOXO_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        TOXO = Y --> bfdcp_ipotd_pregn = 9 Toxoplasmosis (fetal death(s) only)

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "9";

        return value;
    }
    public static string OTHERI_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        OTHERI = Y --> bfdcp_ipotd_pregn = 14 Other

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "14";

        return value;
    }
    public static string TLAB_FET_Rule(string value)
    {
        /*Y = Yes -> 1 Yes
        N = No -> 0 No
        U = Unknown -> 7777 Unknown
        X = Not Applicable -> 2 Not Applicable

        Map empty rows to 9999 (blank)
        */
        switch (value?.ToUpper())
        {
            case "Y":
                value = "1";
                break;
            case "N":
                value = "0";
                break;
            case "U":
                value = "7777";
                break;
            case "X":
                value = "2";
                break;
            default:
                value = "9999";
                break;
        }
        return value;
    }
    public static string MTR_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        MTR = Y --> bfdcp_m_morbi = 0 Maternal transfusion

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "0";

        return value;
    }
    public static string PLAC_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        PLAC = Y --> bfdcp_m_morbi = 3 Third or fourth degree perineal laceration

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "3";

        return value;
    }
    public static string RUT_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        RUT = Y --> bfdcp_m_morbi = 5 Ruptured uterus

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "5";

        return value;
    }
    public static string UHYS_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        UHYS = Y --> bfdcp_m_morbi = 1 Unplanned hysterectomy

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "1";

        return value;
    }
    public static string AINT_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        AINT = Y --> bfdcp_m_morbi = 4 Admission to intensive care unit

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "4";

        return value;
    }
    public static string UOPR_FET_Rule(string value)
    {
        /*Use values from 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] to populate MMRIA multi-select field (bfdcp_m_morbi). 

        UOPR = Y --> bfdcp_m_morbi = 2 Unplanned operating room procedure following delivery

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "N", then bfdcp_m_morbi = 6 None of the above

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is equal to "U" then bfdcp_m_morbi = 7777 Unknown

        If every one of the 6 IJE fields [MTR, PLAC, RUT, UHYS, AINT, UOPR] is empty then bfdcp_m_morbi = 9999 (blank)*/
        if (value == "Y")
            value = "2";

        return value;
    }
    public static string FWG_pound_FET_Rule(string value)
    {
        /*If BWG is in 0000-9998, do the following:
        1. Transfer number verbatim to bcifsbadbw_go_pound.
        2. Set value for bcifsbadbw_uo_measu to 0 Grams.

        If BWG = 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        If BWG > 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        */
        int.TryParse(value, out int numberParsed);

        if (numberParsed >= 9999)
            value = "";

        return value;
    }
    public static string FWG_measure_FET_Rule(string value)
    {
        /*If BWG is in 0000-9998, do the following:
        1. Transfer number verbatim to bcifsbadbw_go_pound.
        2. Set value for bcifsbadbw_uo_measu to 0 Grams.

        If BWG = 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        If BWG > 9999, do the following:
        1. Leave bcifsbadbw_go_pound empty/blank.
        2. Leave bcifsbadbw_uo_measu as 9999 (blank).

        */
        int.TryParse(value, out int numberParsed);

        if (numberParsed >= 9999)
            value = "9999";
        else
            value = "0";

        return value;
    }

    public static string PLUR_Custom_FET_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "1";
                break;
            case "02":
            case "2":
                value = "2";
                break;
            case "03":
            case "3":
                value = "3";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = "4";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }
    public static string PLUR_sigt_FET_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "";
                break;
            case "02":
            case "2":
                value = "";
                break;
            case "03":
            case "3":
                value = "";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = value;
                break;
            default:
                value = "";
                break;
        }

        return value;
    }
    public static string PLUR_gesta_FET_Rule(string value)
    {
        /*If PLUR = 01, then do the following:
        1. Set bfdcppc_plura = 1 Singleton
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 0 No

        If PLUR = 02, then do the following:
        1. Set bfdcppc_plura = 2 Twins
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 03, then do the following:
        1. Set bfdcppc_plura = 3 Triplets
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR is in 04-12, then do the following:
        1. Set bfdcppc_plura = 4 More than 3
        2. Transfer PLUR verbatim to bfdcppc_sigt_3
        3. Set bcifs_im_gesta = 1 Yes

        If PLUR = 99, then do the following:
        1. Set bfdcppc_plura = 9999 (blank)
        2. Leave bfdcppc_sigt_3 empty/blank
        3. Set bcifs_im_gesta = 9999 (blank)*/

        switch (value)
        {
            case "01":
            case "1":
                value = "0";
                break;
            case "02":
            case "2":
                value = "1";
                break;
            case "03":
            case "3":
                value = "1";
                break;
            case "04":
            case "05":
            case "06":
            case "07":
            case "08":
            case "09":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            case "10":
            case "11":
            case "12":
                value = "1";
                break;
            default:
                value = "9999";
                break;
        }

        return value;
    }

    public static string ANEN_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        ANEN = Y --> bcifs_c_anoma = 0 Anencephaly

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "0";

        return value;
    }
    public static string MNSB_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        MNSB = Y --> bcifs_c_anoma = 9 Meningomyelocele or Spina bifida

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "9";

        return value;
    }
    public static string CCHD_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CCHD = Y --> bcifs_c_anoma = 1 Cyanotic congenital heart disease

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "1";

        return value;
    }
    public static string CDH_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CDH = Y --> bcifs_c_anoma = 10 Congenital diaphragmatic hernia

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "10";

        return value;
    }
    public static string OMPH_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        OMPH = Y --> bcifs_c_anoma = 2 Omphalocele

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "2";

        return value;
    }
    public static string GAST_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        GAST = Y --> bcifs_c_anoma = 11 Gastroschisis

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "11";

        return value;
    }
    public static string LIMB_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        LIMB = Y --> bcifs_c_anoma = 3 Limb reduction defect (excluding congenital amputation and dwarfing syndromes)

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "3";

        return value;
    }
    public static string CL_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CL = Y --> bcifs_c_anoma = 4 Cleft Lip with or without Cleft Palate

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "4";

        return value;
    }
    public static string CP_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        CP = Y --> bcifs_c_anoma = 12 Cleft palate alone

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "12";

        return value;
    }
    public static string DOWT_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If DOWT = C --> bcifs_c_anoma = 6 Karyotype confirmed - Downs Syndrome
        If DOWT = P --> bcifs_c_anoma = 7 Karyotype pending - Downs Syndrome

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if(value == "C")
            value = "6";
        else if (value == "P")
            value = "7";

        return value;
    }
    public static string CDIT_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        If CDIT = C --> bcifs_c_anoma = 14 Karyotype confirmed - Suspected chromosomal disorder
        If CDIT = P --> bcifs_c_anoma = 15 Karyotype pending - Suspected chromosomal disorder

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "C")
            value = "14";
        else if (value == "P")
            value = "15";

        return value;
    }
    public static string HYPO_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] to populate MMRIA multi-select field (bcifs_c_anoma). 

        HYPO = Y --> bcifs_c_anoma = 8 Hypospadias

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "N", then bcifs_c_anoma = 17 None of the above

        If every one of the 12 IJE fields [ANEN, MNSB, CCHD, CDH, OMPH, GAST, LIMB, CL, CP, DOWT, CDIT, HYPO] is equal to "U" then bcifs_c_anoma = 7777 Unknown*/
        if (value == "Y")
            value = "8";

        return value;
    }
    public static string MAGER_FET_Rule(string value, string dob_YR, string dob_MO, string dob_day, string dodeliv_YR, string dodeliv_MO, string dodeliv_day)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the MMRIA value empty/blank*/
        if (value == "99")
            value = age_delivery(dob_YR, dob_MO, dob_day, dodeliv_YR, dodeliv_MO, dodeliv_day);

        return value;
    }
    public static string FAGER_FET_Rule(string value, string dob_YR, string dob_MO, string dodeliv_YR, string dodeliv_MO, string dodeliv_day)
    {
        /*If value is in 00-98, transfer number verbatim to MMRIA field.

        If value = 99, leave the MMRIA value empty/blank*/
        if (value == "99")
            value = age_delivery(dob_YR, dob_MO, "1", dodeliv_YR, dodeliv_MO, dodeliv_day);

        return value;
    }
    public static string EHYPE_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        EHYPE = Y --> bfdcprf_rfit_pregn = 4 Eclampsia Hypertension

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "4";

        return value;
    }
    public static string INFT_DRG_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT_DRG = Y --> bfdcprf_rfit_pregn = 8 Fertility Enhancing Drugs, Artificial Insemination or Intrauterine Insemination

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "8";

        return value;
    }
    public static string INFT_ART_FET_Rule(string value)
    {
        /*Use values from 11 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, INFT_DRG, INFT_ART, PPO] to populate MMRIA multi-select field (bfdcprf_rfit_pregn). Note that these 11 IJE fields are not listed sequentially in order in this spreadsheet/IJE ordering.

        INFT_ART = Y --> bfdcprf_rfit_pregn = 9 Assisted Reproductive Technology (e.g. in vitro fertilization (IVF), gamete intrafallopian transfer (GIFT))

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "N", then bfdcprf_rfit_pregn = 11 None of the above

        If every one of the following 9 IJE fields (PDIAB, GDIAB, PHYPE, GHYPE, PPB, INFT, PCES, EHYPE, PPO) is equal to "U" then bfdcprf_rfit_pregn = 7777 Unknown

        *Note that when looking across the multiple fields to fill in "11 None of the above" and "7777 Unknown", you are looking across only 9 fields (not all 11) because INFT_DRG and INFR_ART are part of a skip pattern. */
        if (value == "Y")
            value = "9";

        return value;
    }
    public static string HSV1_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        HSV1 = Y --> bfdcp_ipotd_pregn = 12 Genital Herpes (fetal death only)

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "12";

        return value;
    }
    public static string HIV_FET_Rule(string value)
    {
        /*Use values from 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] to populate MMRIA multi-select field bfdcp_ipotd_pregn). Note that these fields are not ordered sequentially in this spreadsheet.

        HIV = Y --> bfdcp_ipotd_pregn = 13 HIV (fetal death only)

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "N", then bfdcp_ipotd_pregn = 10 None of the above

        If every one of the 12 IJE fields [GON, SYPH, CHAM, LM, GBS, CMV, B19, TOXO, HSV, HSV1, HIV, OTHERI] is equal to "U" then bfdcp_ipotd_pregn = 7777 Unknown*/
        if (value == "Y")
            value = "13";

        return value;
    }
    public static string FBPLACD_ST_TER_C_FET_Rule(string value)
    {
        /*Map XX --> 9999 (blank)
        Map ZZ --> 9999 (blank)

        Map all other values to MMRIA field state listing*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }
    public static string FBPLACE_CNT_C_FET_Rule(string value)
    {
        /*Map to MMRIA field country listing 

        XX --> 9999 (blank)
        ZZ --> 9999 (blank)*/
        if (value == "XX" || value == "ZZ")
            value = "9999";

        return value;
    }
    public static string FETHNIC_FET_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values of FETHNIC1, FETHNIC2, FETHNIC3, FETHNIC4 to populate MMRIA field bfdcpdof_ifoh_origi.

            H --> bfdcpdof_ifoh_origi = 1 Yes, Mexican, Mexican American, Chicano
        H --> bfdcpdof_ifoh_origi = 2 Yes, Puerto Rican
        H --> bfdcpdof_ifoh_origi = 3 Yes, Cuban
        H --> bfdcpdof_ifoh_origi = 4, Yes, Other Spanish/Hispanic/Latino

            If FETHNIC1 = N and FETHNIC2 = N and FETHNIC3 = N and FETHNIC 4 = N --> bfdcpdof_ifoh_origi = 0 No, Not Spanish/Hispanic/Latino

            If FETHNIC1 = U and FETHNIC2 = U and FETHNIC3 = U and FETHNIC4 = U --> bfdcpdof_ifoh_origi = 7777 Unknown

            If FETHNIC1 = (empty) and FETHNIC2 = (empty) and FETHNIC3 = (empty) and FETHNIC4 = (empty) --> bfdcpdof_ifoh_origi = 9999 (blank)*/

        string determinedValue;

        if (value1?.ToUpper() == "H")
        {
            determinedValue = "1";
        }
        else if (value2?.ToUpper() == "H")
        {
            determinedValue = "2";
        }
        else if (value3?.ToUpper() == "H")
        {
            determinedValue = "3";
        }
        else if (value4?.ToUpper() == "H")
        {
            determinedValue = "4";
        }
        else if (value1?.ToUpper() == "N" && value2?.ToUpper() == "N" && value3?.ToUpper() == "N" && value4?.ToUpper() == "N")
        {
            determinedValue = "0";
        }
        else if (value1?.ToUpper() == "U" && value2?.ToUpper() == "U" && value3?.ToUpper() == "U" && value4?.ToUpper() == "U")
        {
            determinedValue = "7777";
        }
        else
        {
            determinedValue = "9999";
        }

        return determinedValue;
    }


    public static string[] FRACE_FET_Rule(string value1, string value2, string value3, string value4, string value5,
        string value6, string value7, string value8, string value9, string value10,
        string value11, string value12, string value13, string value14, string value15)
    {
        /*Use values from FRACE1 through FRACE15 to populate MMRIA multi-select field (bfdcpdofr_ro_fathe).

        FRACE1 = Y --> bfdcpdofr_ro_fathe = 0 White
        FRACE2 = Y --> bfdcpdofr_ro_fathe = 1 Black or African American
        FRACE3 = Y --> bfdcpdofr_ro_fathe = 2 American Indian or Alaska Native
        FRACE4 = Y --> bfdcpdofr_ro_fathe = 7 Asian Indian
        FRACE5 = Y --> bfdcpdofr_ro_fathe = 8 Chinese
        FRACE6 = Y --> bfdcpdofr_ro_fathe = 9 Filipino
        FRACE7 = Y --> bfdcpdofr_ro_fathe = 10 Japanese
        FRACE8 = Y --> bfdcpdofr_ro_fathe = 11 Korean
        FRACE9 = Y --> bfdcpdofr_ro_fathe = 12 Vietnamese
        FRACE10 = Y --> bfdcpdofr_ro_fathe = 13 Other Asian
        FRACE11 = Y --> bfdcpdofr_ro_fathe = 3 Native Hawaiian
        FRACE12 = Y --> bfdcpdofr_ro_fathe = 4 Guamanian or Chamorro
        FRACE13 = Y --> bfdcpdofr_ro_fathe = 5 Samoan
        FRACE14 = Y --> bfdcpdofr_ro_fathe = 6 Other Pacific Islander
        FRACE15 = Y --> bfdcpdofr_ro_fathe = 14 Other Race

        If every one of FRACE1 through FRACE15 is equal to "N", then bfdcpdofr_ro_fathe = 8888 (Race Not Specified)*/
        List<string> determinedValues = new List<string>();


        if (value1?.ToUpper() == "Y")
        {
            determinedValues.Add("0");
        }
        if (value2?.ToUpper() == "Y")
        {
            determinedValues.Add("1");
        }
        if (value3?.ToUpper() == "Y")
        {
            determinedValues.Add("2");
        }
        if (value4?.ToUpper() == "Y")
        {
            determinedValues.Add("7");
        }
        if (value5?.ToUpper() == "Y")
        {
            determinedValues.Add("8");
        }
        if (value6?.ToUpper() == "Y")
        {
            determinedValues.Add("9");
        }
        if (value7?.ToUpper() == "Y")
        {
            determinedValues.Add("10");
        }
        if (value8?.ToUpper() == "Y")
        {
            determinedValues.Add("11");
        }
        if (value9?.ToUpper() == "Y")
        {
            determinedValues.Add("12");
        }
        if (value10?.ToUpper() == "Y")
        {
            determinedValues.Add("13");
        }
        if (value11?.ToUpper() == "Y")
        {
            determinedValues.Add("3");
        }
        if (value12?.ToUpper() == "Y")
        {
            determinedValues.Add("4");
        }
        if (value13?.ToUpper() == "Y")
        {
            determinedValues.Add("5");
        }
        if (value14?.ToUpper() == "Y")
        {
            determinedValues.Add("6");
        }
        if (value15?.ToUpper() == "Y")
        {
            determinedValues.Add("14");
        }
        if(determinedValues.Count == 0)
        {
            determinedValues.Add("8888");
        }
        return determinedValues.ToArray();
    }

    public static string FRACE16_17_FET_Rule(string value16, string value17)
    {
        /*Combine FRACE16 and FRACE17 into one field (bfdcpdofr_p_tribe), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE16 to MMRIA field.
        2. Transfer string verbatim from FRACE17 and add to same MMRIA field.
        3. If both FRACE16 and FRACE17 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value16) || string.IsNullOrWhiteSpace(value17)))
        {
            value = $"{value16}|{value17}";
        }
        else if (!string.IsNullOrWhiteSpace(value16))
        {
            value = $"{value16}";
        }
        else
        {
            value = $"{value17}";
        }

        return value;
    }

    public static string FRACE18_19_FET_Rule(string value18, string value19)
    {
        /*Combine FRACE18 and FRACE19 into one field (bfdcpdofr_o_asian), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE18 to MMRIA field.
        2. Transfer string verbatim from FRACE19 and add to same MMRIA field.
        3. If both FRACE18 and FRACE19 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value18) || string.IsNullOrWhiteSpace(value19)))
        {
            value = $"{value18}|{value19}";
        }
        else if (!string.IsNullOrWhiteSpace(value18))
        {
            value = $"{value18}";
        }
        else
        {
            value = $"{value19}";
        }

        return value;
    }

    public static string FRACE20_21_FET_Rule(string value20, string value21)
    {
        /*Combine FRACE20 and FRACE21 into one field (bfdcpdofr_op_islan), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE20 to MMRIA field.
        2. Transfer string verbatim from FRACE21 and add to same MMRIA field.
        3. If both FRACE20 and FRACE21 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value20) || string.IsNullOrWhiteSpace(value21)))
        {
            value = $"{value20}|{value21}";
        }
        else if (!string.IsNullOrWhiteSpace(value20))
        {
            value = $"{value20}";
        }
        else
        {
            value = $"{value21}";
        }

        return value;
    }

    public static string FRACE22_23_FET_Rule(string value22, string value23)
    {
        /*Combine FRACE22 and FRACE23 into one field (bfdcpdofr_o_race), separated by pipe delimiter. 

        1. Transfer string verbatim from FRACE22 to MMRIA field.
        2. Transfer string verbatim from FRACE23 and add to same MMRIA field.
        3. If both FRACE22 and FRACE23 are empty, leave MMRIA field empty (blank).*/
        string value = string.Empty;

        if (!(string.IsNullOrWhiteSpace(value22) || string.IsNullOrWhiteSpace(value23)))
        {
            value = $"{value22}|{value23}";
        }
        else if (!string.IsNullOrWhiteSpace(value22))
        {
            value = $"{value22}";
        }
        else
        {
            value = $"{value23}";
        }

        return value;
    }

    public static string FET_METHNIC_Rule(string value1, string value2, string value3, string value4)
    {
        /*Use values of METHNIC1, METHNIC2, METHNIC3, METHNIC4 to populate MMRIA field bfdcpdom_ioh_origi.

        H --> bfdcpdom_ioh_origi = 1 Yes, Mexican, Mexican American, Chicano
        H --> bfdcpdom_ioh_origi = 2 Yes, Puerto Rican
        H --> bfdcpdom_ioh_origi = 3 Yes, Cuban
        H --> bfdcpdom_ioh_origi = 4 Yes, Other Spanish/Hispanic/Latino

        If METHNIC1 = N and METHNIC2 = N and METHNIC3 = N and METHNIC 4 = N --> bfdcpdom_ioh_origi = 0 No, Not Spanish/Hispanic/Latino

        If METHNIC1 = U and METHNIC2 = U and METHNIC3 = U and METHNIC4 = U --> bfdcpdom_ioh_origi = 7777 Unknown

        If METHNIC1 = (empty) and METHNIC2 = (empty) and METHNIC3 = (empty) and METHNIC4 = (empty) --> bfdcpdom_ioh_origi = 9999 (blank)*/

        string determinedValue;

        if (value1?.ToUpper() == "H")
        {
            determinedValue = "1";
        }
        else if (value2?.ToUpper() == "H")
        {
            determinedValue = "2";
        }
        else if (value3?.ToUpper() == "H")
        {
            determinedValue = "3";
        }
        else if (value4?.ToUpper() == "H")
        {
            determinedValue = "4";
        }
        else if (value1?.ToUpper() == "N" && value2?.ToUpper() == "N" && value3?.ToUpper() == "N" && value4?.ToUpper() == "N")
        {
            determinedValue = "0";
        }
        else if (value1?.ToUpper() == "U" && value2?.ToUpper() == "U" && value3?.ToUpper() == "U" && value4?.ToUpper() == "U")
        {
            determinedValue = "7777";
        }
        else
        {
            determinedValue = "9999";
        }

        return determinedValue;
    }


}
