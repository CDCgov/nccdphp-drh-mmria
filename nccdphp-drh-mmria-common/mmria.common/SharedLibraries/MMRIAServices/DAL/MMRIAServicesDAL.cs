using System;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.MMRIAServices.DAL;

public sealed class MMRIAServicesDAL
{
    private readonly CouchDbHttpClient _couchDbHttpClient;

    public MMRIAServicesDAL()
    {
    }

    public MMRIAServicesDAL(CouchDbHttpClient couchDbHttpClient)
    {
        _couchDbHttpClient = couchDbHttpClient;
    }

    public async Task<case_view_response> GetCaseView(DBConfigurationDetail db_info, string search_key)
    {
        string request_string = $"{db_info.url}/{db_info.prefix}mmrds/_design/sortable/_view/by_last_name?skip=0&limit=100000&startkey=\"{search_key.ToLower()}\"&endkey=\"{search_key.ToUpper()}\"";

        try
        {
            string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, db_info.user_name, db_info.user_value, timeoutSeconds: 300);

            case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<case_view_response>(responseFromServer);


            string key_compare = search_key.ToLower().Trim(new char[] { '"' });

            case_view_response result = new case_view_response();
            result.offset = case_view_response.offset;
            result.total_rows = case_view_response.total_rows;

            foreach (case_view_item cvi in case_view_response.rows)
            {
                bool add_item = false;

                if (is_matching_search_text(cvi.value.last_name, key_compare))
                {
                    add_item = true;
                }

                if (add_item)
                {
                    result.rows.Add(cvi);
                }

            }


            result.total_rows = result.rows.Count;
            result.rows = result.rows.Skip(0).Take(100000).ToList();

            return result;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"MMRIAServicesDAL GetCaseView\nurl: {request_string}\n\nerror:\n{ex}");

        }


        return null;
    }

    public async Task<ExpandoObject> GetCaseById(DBConfigurationDetail db_info, string case_id)
    {
        try
        {
            string request_string = $"{db_info.url}/{db_info.prefix}mmrds/_all_docs?include_docs=true";

            if (!string.IsNullOrWhiteSpace(case_id))
            {
                request_string = $"{db_info.url}/{db_info.prefix}mmrds/{case_id}";
                string responseFromServer = await _couchDbHttpClient.ExecuteAsync("GET", request_string, null, db_info.user_name, db_info.user_value);

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(responseFromServer);

                return result;

            }

        }
        catch (Exception ex)
        {

            Console.WriteLine($"MMRIAServicesDAL.GetCaseById\n{ex}");
        }

        return null;
    }

    private bool is_matching_search_text(string p_val1, string p_val2)
    {
        var result = false;

        if
        (
            !string.IsNullOrWhiteSpace(p_val1) &&
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

    public string GetConfigurationDocumentJson(
        string couchDbUrl,
        string configId,
        string userName,
        string password
    )
    {
        string requestUrl = $"{couchDbUrl}/configuration/{configId}";

        using var httpClient = new HttpClient();
        string auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{userName}:{password}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        return httpClient.GetStringAsync(requestUrl).GetAwaiter().GetResult();
    }
}