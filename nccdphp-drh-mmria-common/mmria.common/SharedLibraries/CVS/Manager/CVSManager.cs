using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mmria.common.couchdb;
using mmria.common.cvs;
using mmria.common.SharedLibraries.CVS.DAL;
using mmria.common.SharedLibraries.CVS.Model;

namespace mmria.common.SharedLibraries.CVS.Manager;

public sealed class CVSManager
{
    private readonly CVSDAL _dal;
    private readonly ILogger<CVSManager> _logger;

    public CVSManager(CVSDAL dal, ILogger<CVSManager> logger)
    {
        _dal = dal;
        _logger = logger;
    }

    public async Task<string> GetServerStatusAsync(CVSConfigurationDetail cvs)
    {
        return await _dal.PostExternalAsync(cvs.cvs_api_url, new server_status_post_body
        {
            id = cvs.cvs_api_id,
            secret = cvs.cvs_api_key
        });
    }

    public async Task<tract_county_result> GetAllDataAsync(post_payload post_payload, CVSConfigurationDetail cvs)
    {
        var get_all_data_body = new get_all_data_post_body()
        {
            id = cvs.cvs_api_id,
            secret = cvs.cvs_api_key,
            payload = new()
            {
                c_geoid = post_payload.c_geoid,
                t_geoid = post_payload.t_geoid,
                year = post_payload.year
            }
        };

        if (!string.IsNullOrWhiteSpace(get_all_data_body.payload.year))
        {
            int test_year = -1;
            int selected_year = -1;

            if (int.TryParse(get_all_data_body.payload.year, out test_year))
            {
                selected_year = test_year;
                try
                {
                    var get_year_response = await _dal.PostExternalAsync(cvs.cvs_api_url, new get_year_post_body
                    {
                        id = cvs.cvs_api_id,
                        secret = cvs.cvs_api_key,
                        payload = new()
                    });

                    var valid_year_list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(get_year_response);
                    const int year_difference_limit = 9;
                    if (valid_year_list != null && valid_year_list.Count > 0 && !valid_year_list.Contains(selected_year))
                    {
                        var lower_diff = Math.Abs(valid_year_list[0] - selected_year);
                        var upper_diff = Math.Abs(valid_year_list[valid_year_list.Count - 1] - selected_year);
                        if (lower_diff < upper_diff)
                        {
                            if (lower_diff <= year_difference_limit)
                            {
                                get_all_data_body.payload.year = valid_year_list[0].ToString();
                            }
                        }
                        else if (upper_diff <= year_difference_limit)
                        {
                            get_all_data_body.payload.year = valid_year_list[valid_year_list.Count - 1].ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Year query failed — proceed with the original year value.
                    _logger.LogWarning(ex, "CVS year-list query failed; proceeding with original year value.");
                }
            }
        }

        try
        {
            var response_string = await _dal.PostExternalAsync(cvs.cvs_api_url, get_all_data_body);
            return JsonSerializer.Deserialize<tract_county_result>(response_string);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CVS GetAllData failed.");
            return null;
        }
    }

    public async Task<CVSFileStatusResult> GetDashboardAsync(post_payload post_payload, CVSConfigurationDetail cvs, DBConfigurationDetail db_config)
    {
        const int year_difference_limit = 9;
        var file_status_result = new CVSFileStatusResult();

        var get_dashboard_body = new get_dashboard_post_body()
        {
            id = cvs.cvs_api_id,
            secret = cvs.cvs_api_key,
            payload = new()
            {
                lat = post_payload.lat,
                lon = post_payload.lon,
                year = post_payload.year,
                id = post_payload.id
            }
        };

        if (string.IsNullOrWhiteSpace(get_dashboard_body.payload.lat))
        {
            try
            {
                var case_view_response = await _dal.GetCaseViewByRecordIdAsync(post_payload.id, db_config);
                var data = case_view_response.rows.Count > 0 ? case_view_response.rows[0] : null;
                var case_dictionary = data == null ? null : await _dal.GetCaseAsync(data.id, db_config) as IDictionary<string, object>;

                if (string.IsNullOrWhiteSpace(get_dashboard_body.payload.year) && case_dictionary != null && case_dictionary.ContainsKey("home_record"))
                {
                    var home_record = case_dictionary["home_record"] as IDictionary<string, object>;
                    if (home_record != null && home_record.ContainsKey("date_of_death"))
                    {
                        var date_of_death = home_record["date_of_death"] as IDictionary<string, object>;
                        if (date_of_death != null && date_of_death.ContainsKey("year") && date_of_death["year"] != null)
                        {
                            get_dashboard_body.payload.year = date_of_death["year"].ToString();
                        }
                        else
                        {
                            file_status_result.is_valid_address = false;
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(get_dashboard_body.payload.year))
                    {
                        file_status_result.is_valid_address = false;
                    }
                }
                else if (string.IsNullOrWhiteSpace(get_dashboard_body.payload.year))
                {
                    file_status_result.is_valid_address = false;
                }

                if (string.IsNullOrWhiteSpace(get_dashboard_body.payload.lat) && case_dictionary != null && case_dictionary.ContainsKey("death_certificate"))
                {
                    var death_certificate = case_dictionary["death_certificate"] as IDictionary<string, object>;
                    if (death_certificate != null && death_certificate.ContainsKey("place_of_last_residence"))
                    {
                        var place_of_last_residence = death_certificate["place_of_last_residence"] as IDictionary<string, object>;
                        if (place_of_last_residence != null &&
                            place_of_last_residence.ContainsKey("latitude") &&
                            place_of_last_residence.ContainsKey("longitude") &&
                            place_of_last_residence["latitude"] != null &&
                            place_of_last_residence["longitude"] != null)
                        {
                            get_dashboard_body.payload.lat = place_of_last_residence["latitude"].ToString();
                            get_dashboard_body.payload.lon = place_of_last_residence["longitude"].ToString();
                        }
                        else
                        {
                            file_status_result.is_valid_address = false;
                        }
                    }
                }
                else
                {
                    file_status_result.is_valid_address = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        if (!string.IsNullOrWhiteSpace(get_dashboard_body.payload.year))
        {
            int test_year = -1;
            int selected_year = -1;

            if (int.TryParse(get_dashboard_body.payload.year, out test_year))
            {
                selected_year = test_year;
                var get_year_response = await _dal.PostInternalAsync(cvs.cvs_api_url, new get_year_post_body
                {
                    id = cvs.cvs_api_id,
                    secret = cvs.cvs_api_key,
                    payload = new()
                }, db_config);

                var valid_year_list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(get_year_response);
                if (valid_year_list != null && valid_year_list.Count > 0 && !valid_year_list.Contains(selected_year))
                {
                    var lower_diff = Math.Abs(valid_year_list[0] - selected_year);
                    var upper_diff = Math.Abs(valid_year_list[valid_year_list.Count - 1] - selected_year);

                    if (lower_diff < upper_diff)
                    {
                        if (lower_diff <= year_difference_limit)
                        {
                            get_dashboard_body.payload.year = valid_year_list[0].ToString();
                        }
                        else
                        {
                            file_status_result.is_valid_year = false;
                        }
                    }
                    else
                    {
                        if (upper_diff <= year_difference_limit)
                        {
                            get_dashboard_body.payload.year = valid_year_list[valid_year_list.Count - 1].ToString();
                        }
                        else
                        {
                            file_status_result.is_valid_year = false;
                        }
                    }
                }
            }
            else
            {
                file_status_result.is_valid_year = false;
            }
        }

        if (!file_status_result.is_valid_address || !file_status_result.is_valid_year)
        {
            file_status_result.file_status = "Validation Error";
            return file_status_result;
        }

        if (string.IsNullOrWhiteSpace(get_dashboard_body.payload.year) ||
            string.IsNullOrWhiteSpace(get_dashboard_body.payload.lat) ||
            string.IsNullOrWhiteSpace(get_dashboard_body.payload.lon))
        {
            file_status_result.is_valid_address = false;
            file_status_result.file_status = "Validation Error";
            return file_status_result;
        }

        file_status_result.updated_lat = get_dashboard_body.payload.lat;
        file_status_result.updated_lon = get_dashboard_body.payload.lon;
        file_status_result.updated_year = get_dashboard_body.payload.year;

        CVSExternalResponse externalResponse;
        try
        {
            externalResponse = await _dal.PostExternalForResponseAsync(cvs.cvs_api_url, get_dashboard_body);
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException || ex is TaskCanceledException)
        {
            file_status_result.file_status = "unavailable";
            file_status_result.message = "The CVS service did not respond.";
            return file_status_result;
        }

        if (!externalResponse.IsSuccess)
        {
            file_status_result.file_status = IsTransientHttpStatus(externalResponse.StatusCode) ? "unavailable" : "error";
            file_status_result.message = $"The CVS service returned HTTP {externalResponse.StatusCode}.";
            return file_status_result;
        }

        if (string.IsNullOrWhiteSpace(externalResponse.Body))
        {
            file_status_result.file_status = "unavailable";
            file_status_result.message = "The CVS service returned an empty response.";
            return file_status_result;
        }

        IDictionary<string, object> responseDictionary;
        try
        {
            responseDictionary = JsonSerializer.Deserialize<ExpandoObject>(externalResponse.Body) as IDictionary<string, object>;
        }
        catch (JsonException)
        {
            if (IsGeneratingResponse(externalResponse.Body))
            {
                file_status_result.file_status = "generating";
                file_status_result.message = "The CVS service is preparing the PDF.";
            }
            else if (LooksLikeUnavailableResponse(externalResponse.Body))
            {
                file_status_result.file_status = "unavailable";
                file_status_result.message = "The CVS service is unavailable.";
            }
            else
            {
                file_status_result.file_status = "error";
                file_status_result.message = "The CVS service returned an unexpected response.";
            }
            return file_status_result;
        }

        if (responseDictionary != null)
        {
            if (responseDictionary.ContainsKey("isBase64Encoded") &&
                responseDictionary["isBase64Encoded"] != null &&
                responseDictionary["isBase64Encoded"].ToString() == "True")
            {
                try
                {
                    file_status_result.PdfBytes = Convert.FromBase64String(GetResponseValueAsString(responseDictionary["body"]));
                    file_status_result.file_status = "file ready";
                }
                catch (FormatException)
                {
                    file_status_result.file_status = "error";
                    file_status_result.message = "The CVS service returned an invalid PDF response.";
                }
            }
            else if (responseDictionary.ContainsKey("body") && IsGeneratingResponse(GetResponseValueAsString(responseDictionary["body"])))
            {
                file_status_result.file_status = "generating";
            }
            else
            {
                file_status_result.file_status = "error";
            }
        }
        else
        {
            file_status_result.file_status = "error";
        }

        return file_status_result;
    }

    private static bool IsTransientHttpStatus(int statusCode) =>
        statusCode == 408 || statusCode == 429 || statusCode == 500 ||
        statusCode == 503 || statusCode == 504;

    private static bool IsGeneratingResponse(string body) =>
        body != null && (
            body.Contains("PDF ", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("PDF creation has been initiated", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("retry API call", StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeUnavailableResponse(string body) =>
        !string.IsNullOrWhiteSpace(body) && (
            body.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("temporarily", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("service unavailable", StringComparison.OrdinalIgnoreCase));

    private static string GetResponseValueAsString(object value)
    {
        if (value is JsonElement elem)
        {
            return elem.ValueKind == JsonValueKind.String
                ? elem.GetString() ?? string.Empty
                : elem.GetRawText();
        }
        return value?.ToString() ?? string.Empty;
    }
}
