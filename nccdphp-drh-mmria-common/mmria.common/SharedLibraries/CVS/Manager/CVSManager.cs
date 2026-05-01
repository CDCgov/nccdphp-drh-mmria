using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using mmria.common.couchdb;
using mmria.common.cvs;
using mmria.common.SharedLibraries.CVS.DAL;
using mmria.common.SharedLibraries.CVS.Model;

namespace mmria.common.SharedLibraries.CVS.Manager;

public sealed class CVSManager
{
    private readonly CVSDAL _dal;

    public CVSManager(CVSDAL dal)
    {
        _dal = dal;
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
        }

        var response_string = await _dal.PostExternalAsync(cvs.cvs_api_url, get_all_data_body);
        return JsonSerializer.Deserialize<tract_county_result>(response_string);
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
        catch (HttpRequestException)
        {
            file_status_result.file_status = "unavailable";
            file_status_result.message = "The CVS service did not respond.";
            return file_status_result;
        }
        catch (TaskCanceledException)
        {
            file_status_result.file_status = "unavailable";
            file_status_result.message = "The CVS service request timed out.";
            return file_status_result;
        }

        if (!externalResponse.IsSuccessStatusCode)
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

        IDictionary<string, object> responseDictionary = null;
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
            var isBase64Encoded = responseDictionary.ContainsKey("isBase64Encoded")
                ? GetResponseValueAsString(responseDictionary["isBase64Encoded"])
                : null;
            var body = responseDictionary.ContainsKey("body")
                ? GetResponseValueAsString(responseDictionary["body"])
                : null;

            if (responseDictionary.ContainsKey("isBase64Encoded") &&
                string.Equals(isBase64Encoded, "true", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(body))
                {
                    file_status_result.file_status = "error";
                    file_status_result.message = "The CVS service returned an empty PDF response.";
                }
                else
                {
                    try
                    {
                        file_status_result.PdfBytes = Convert.FromBase64String(body);
                        file_status_result.file_status = "file ready";
                    }
                    catch (FormatException)
                    {
                        file_status_result.file_status = "error";
                        file_status_result.message = "The CVS service returned an invalid PDF response.";
                    }
                }
            }
            else if (IsGeneratingResponse(body))
            {
                file_status_result.file_status = "generating";
                file_status_result.message = "The CVS service is preparing the PDF.";
            }
            else if (LooksLikeUnavailableResponse(body))
            {
                file_status_result.file_status = "unavailable";
                file_status_result.message = "The CVS service is unavailable.";
            }
            else
            {
                file_status_result.file_status = "error";
                file_status_result.message = "The CVS service returned an unexpected response.";
            }
        }
        else
        {
            file_status_result.file_status = "error";
        }

        return file_status_result;
    }

    private static bool IsTransientHttpStatus(int statusCode)
    {
        return statusCode == 408 ||
            statusCode == 429 ||
            statusCode == 500 ||
            statusCode == 502 ||
            statusCode == 503 ||
            statusCode == 504;
    }

    private static bool IsGeneratingResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        return body.StartsWith("PDF ", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("PDF is being created", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("PDF creation has been initiated", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("retry API call", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeUnavailableResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        return body.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("temporarily", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("service unavailable", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetResponseValueAsString(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind == JsonValueKind.String
                ? jsonElement.GetString()
                : jsonElement.ToString();
        }

        return value.ToString();
    }
}
