using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.model.couchdb;
using mmria.common.SharedLibraries.VitalImport.DAL;
using mmria.common.SharedLibraries.VitalImport.Model;

namespace mmria.common.SharedLibraries.VitalImport.Manager;

public sealed class VitalImportManager
{
    private readonly VitalImportDAL _dal;

    public VitalImportManager(VitalImportDAL dal)
    {
        _dal = dal;
    }

    public async Task<case_view_response> GetCaseViewAsync(string search_key, DBConfigurationDetail db_config)
    {
        var couchdb_max_take_value = 268_435_456;
        int skip = 0;
        int take = couchdb_max_take_value;
        string sort = "by_last_name";
        bool descending = false;

        string sort_view = sort.ToLower();
        switch (sort_view)
        {
            case "by_date_created":
            case "by_date_last_updated":
            case "by_last_name":
            case "by_first_name":
            case "by_middle_name":
            case "by_year_of_death":
            case "by_month_of_death":
            case "by_committee_review_date":
            case "by_created_by":
            case "by_last_updated_by":
            case "by_state_of_death":
            case "by_date_last_checked_out":
            case "by_last_checked_out_by":
            case "by_case_status":
                break;
            default:
                sort_view = "by_date_created";
                break;
        }

        System.Text.StringBuilder request_builder = new System.Text.StringBuilder();
        request_builder.Append($"{db_config.url}/{db_config.prefix}mmrds/_design/sortable/_view/{sort_view}?");

        if (skip > -1)
        {
            request_builder.Append($"skip={skip}");
        }
        else
        {
            request_builder.Append("skip=0");
        }

        if (take > -1)
        {
            request_builder.Append($"&limit={take}");
        }

        if (descending)
        {
            request_builder.Append("&descending=true");
        }

        case_view_response case_view_response = await _dal.GetCaseViewAsync(request_builder.ToString(), db_config);

        string key_compare = search_key.ToLower().Trim(new char[] { '"' });

        case_view_response result = new case_view_response();
        result.offset = case_view_response.offset;
        result.total_rows = case_view_response.total_rows;

        foreach (case_view_item cvi in case_view_response.rows)
        {
            bool add_item = false;

            if (IsMatchingSearchText(cvi.value.last_name, key_compare))
            {
                add_item = true;
            }

            if (add_item)
            {
                result.rows.Add(cvi);
            }
        }

        result.total_rows = result.rows.Count;
        result.rows = result.rows.Skip(skip).Take(take).ToList();

        return result;
    }

    public async Task<ExpandoObject> GetCaseAsync(string case_id, ClaimsPrincipal user, DBConfigurationDetail db_config)
    {
        if (string.IsNullOrWhiteSpace(case_id))
        {
            return null;
        }

        var result = await _dal.GetCaseAsync(case_id, db_config);

        if (mmria.common.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, user, mmria.common.SharedLibraries.Other.ResourceRightEnum.ReadCase, result))
        {
            return result;
        }

        return null;
    }

    public async Task<VitalImportSaveResult> SaveCaseAsync(ExpandoObject case_post_request, ClaimsPrincipal user, DBConfigurationDetail db_config)
    {
        string object_string = null;
        document_put_response result = new document_put_response();

        var userName = "";
        if (user.Identities.Any(u => u.IsAuthenticated))
        {
            userName = user.Identities.First(
                u => u.IsAuthenticated &&
                u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;
        }

        var byName = (IDictionary<string, object>)case_post_request;
        var created_by = byName["created_by"] as string;
        if (string.IsNullOrWhiteSpace(created_by))
        {
            byName["created_by"] = userName;
        }

        if (byName.ContainsKey("last_updated_by"))
        {
            byName["last_updated_by"] = userName;
        }
        else
        {
            byName.Add("last_updated_by", userName);
        }

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        object_string = Newtonsoft.Json.JsonConvert.SerializeObject(case_post_request, settings);

        var temp_id = byName["_id"];
        string id_val = null;

        if (temp_id is DateTime)
        {
            id_val = string.Concat(((DateTime)temp_id).ToString("s"), "Z");
        }
        else
        {
            id_val = temp_id.ToString();
        }

        var home_record = (IDictionary<string, object>)byName["home_record"];
        if (!home_record.ContainsKey("jurisdiction_id"))
        {
            home_record.Add("jurisdiction_id", "/");
        }

        if (!mmria.common.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, user, mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteCase, home_record["jurisdiction_id"].ToString()))
        {
            Console.Write($"unauthorized PUT {home_record["jurisdiction_id"]}: {byName["_id"]}");
            return new VitalImportSaveResult { Id = id_val, SerializedDocument = object_string, Response = result };
        }

        try
        {
            var check_document_expando_object = await _dal.GetCaseAsync(id_val, db_config);
            IDictionary<string, object> result_dictionary = check_document_expando_object as IDictionary<string, object>;

            if
            (
                result_dictionary != null &&
                !mmria.common.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, user, mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteCase, check_document_expando_object)
            )
            {
                Console.Write($"unauthorized PUT {result_dictionary["jurisdiction_id"]}: {result_dictionary["_id"]}");
                return new VitalImportSaveResult { Id = id_val, SerializedDocument = object_string, Response = result };
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"err caseController.Post\n{ex}");
        }

        result = await _dal.PutCaseAsync(id_val, object_string, db_config);
        return new VitalImportSaveResult
        {
            Id = id_val,
            SerializedDocument = object_string,
            Response = result
        };
    }

    public async Task<VitalImportDeleteResult> DeleteCaseAsync(string case_id, string rev, ClaimsPrincipal user, DBConfigurationDetail db_config)
    {
        if (string.IsNullOrWhiteSpace(case_id) || string.IsNullOrWhiteSpace(rev))
        {
            return null;
        }

        string request_string = db_config.url + $"/{db_config.prefix}mmrds/" + case_id + "?rev=" + rev;
        string document_json = null;

        try
        {
            var check_document_expando_object = await _dal.GetCaseAsync(case_id, db_config);
            document_json = Newtonsoft.Json.JsonConvert.SerializeObject(check_document_expando_object);
            IDictionary<string, object> result_dictionary = check_document_expando_object as IDictionary<string, object>;

            if
            (
                result_dictionary != null &&
                !mmria.common.utils.authorization_case.is_authorized_to_handle_jurisdiction_id(db_config, user, mmria.common.SharedLibraries.Other.ResourceRightEnum.WriteCase, check_document_expando_object)
            )
            {
                Console.Write($"unauthorized DELETE {result_dictionary["jurisdiction_id"]}: {result_dictionary["_id"]}");
                return null;
            }

            if (result_dictionary.ContainsKey("_rev"))
            {
                request_string = db_config.url + $"/{db_config.prefix}mmrds/" + case_id + "?rev=" + result_dictionary["_rev"];
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"err caseController.Delete\n{ex}");
        }

        var result = await _dal.DeleteCaseAsync(request_string, db_config);
        return new VitalImportDeleteResult
        {
            CaseId = case_id,
            DocumentJson = document_json,
            Response = result
        };
    }

    public async Task<alldocs_response<mmria.common.ije.Batch>> GetBatchSetAsync(DBConfigurationDetail db_config)
    {
        return await _dal.GetBatchSetAsync(db_config);
    }

    private bool IsMatchingSearchText(string p_val1, string p_val2)
    {
        var result = false;

        if
        (
            !string.IsNullOrWhiteSpace(p_val1) &&
            p_val1.Length > 3 &&
            (
                p_val2.IndexOf(p_val1, StringComparison.OrdinalIgnoreCase) > -1 ||
                p_val1.IndexOf(p_val2, StringComparison.OrdinalIgnoreCase) > -1
            )
        )
        {
            result = true;
        }

        return result;
    }
}
