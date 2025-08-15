using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;   
using mmria.common.cvs;

namespace mmria.server;

[Authorize]
[Route("api/[controller]")]
public sealed class cvsAPIController: ControllerBase 
{ 

    public sealed class CVS_File_Status
    {
        public CVS_File_Status () {}

        public string file_status { get;set; }
        public string updated_lat { get;set; }
        public string updated_lon { get;set; }

        public string updated_year { get;set; }

        public bool is_valid_address { get;set; } = true;
        public bool is_valid_year { get;set; } = true;

    }

    string folder_name = null;

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    public cvsAPIController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        db_config = configuration.GetDBConfig(host_prefix);

        this.folder_name = System.IO.Path.Combine(configuration.GetString("export_directory", host_prefix), "csv");

        System.IO.Directory.CreateDirectory(this.folder_name);

    }


    [Authorize(Roles  = "abstractor,data_analyst,committee_member")]
    [HttpGet("{id}")]
    public async System.Threading.Tasks.Task<ActionResult> Get (string id)
    {


        var file_name = $"CVS-{id}.pdf";
        var file_path = System.IO.Path.Combine(folder_name, file_name);

        if(System.IO.File.Exists(file_path))
        {
            byte[] fileBytes = await GetFile(file_path);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, file_name);
        }
        else
        {
            return NotFound();
        }


    }


    
    [Authorize(Roles  = "abstractor,data_analyst,committee_member")]
    [HttpPost]
    public async Task<IActionResult> Post
    (
        [FromBody] post_payload post_payload
    ) 
    { 
        var is_abstractor = false;

        foreach(var role in User.Identities.First(u => u.IsAuthenticated &&  u.HasClaim(c => c.Type == ClaimTypes.Name)).Claims.Where(c=> c.Type == ClaimTypes.Role))
        {
            switch(role.Value)
            {
                case "abstractor":
                    is_abstractor = true;
                break;

            }
        }
  

        const int year_difference_limit = 9;

        IActionResult result = null;
        var response_string = string.Empty;
        System.Collections.Generic.IDictionary<string,object> responseDictionary = null;
        var cvs = configuration.GetCVSConfigurationDetail();

        var base_url = cvs.cvs_api_url;

        try
        {
            

            switch(post_payload.action)
            {
                case "server":
                    var sever_status_body = new server_status_post_body()
                    {
                        id = cvs.cvs_api_id,
                        secret = cvs.cvs_api_key,

                    };

                    var body_text = JsonSerializer.Serialize(sever_status_body);
                    var server_statu_curl = new mmria.server.cURL("POST", null, base_url, body_text);

                    response_string = await server_statu_curl.executeAsync();
                    System.Console.WriteLine(response_string);

                    result = Ok(response_string);

    
                break;
                case "data":
                    if(is_abstractor)
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
                                /*
                                c_geoid = "13089",
                                t_geoid = "13089021204",
                                year = "2012"*/
                            }
                        };

                        if(!string.IsNullOrWhiteSpace(get_all_data_body.payload.year))
                        {

                            int test_year = -1;
                            int selected_year = -1;
                            
                            if(int.TryParse(get_all_data_body.payload.year, out test_year))
                            {
                                selected_year = test_year;

                                var get_year_body = new get_year_post_body()
                                {
                                    id = cvs.cvs_api_id,
                                    secret = cvs.cvs_api_key,
                                    payload = new()
                                };

                                body_text = JsonSerializer.Serialize(get_year_body);
                                var get_year_curl = new cURL("POST", null, base_url, body_text, db_config.user_name, db_config.user_value);
                                string get_year_response = await get_year_curl.executeAsync();
                                var valid_year_list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>> (get_year_response);
                                if
                                (
                                    valid_year_list != null &&
                                    valid_year_list.Count > 0 &&
                                    ! valid_year_list.Contains(selected_year)
                                )
                                {

                                    var lower_diff = System.Math.Abs(valid_year_list[0] - selected_year);
                                    var upper_diff = System.Math.Abs(valid_year_list[valid_year_list.Count -1] - selected_year);

                                    if(lower_diff < upper_diff)
                                    {
                                        if(lower_diff <= year_difference_limit)
                                        {
                                            get_all_data_body.payload.year = valid_year_list[0].ToString();
                                        }/*
                                        else
                                        {
                                            file_status_result.is_valid_year = false;
                                        }*/
                                    }
                                    else
                                    {
                                        if(upper_diff <= year_difference_limit)
                                        {
                                            get_all_data_body.payload.year = valid_year_list[valid_year_list.Count -1].ToString();
                                        }
                                        /*
                                        else
                                        {
                                            file_status_result.is_valid_year = false;
                                        }*/
                                    }
                                    
                                }
                            }
                            /*
                            else
                            {
                                file_status_result.is_valid_year = false;
                            }*/
                        }


                        body_text = JsonSerializer.Serialize(get_all_data_body);
                        var get_all_data_curl = new mmria.server.cURL("POST", null, base_url, body_text);

                        response_string = await get_all_data_curl.executeAsync();
                        System.Console.WriteLine(response_string);

                        var tc = JsonSerializer.Deserialize<tract_county_result>(response_string);

                        result =  Ok(tc);

        
                    }

                    break;

                case "dashboard":

                    var file_status_result = new CVS_File_Status();

                    var get_dashboard_body = new get_dashboard_post_body()
                    {
                        id = cvs.cvs_api_id,
                        secret = cvs.cvs_api_key,
                        payload = new()
                        {
                            lat = post_payload.lat,
                            lon = post_payload.lon, 
                            year= post_payload.year,
                            id = post_payload.id
                        }
                    };




                    if(string.IsNullOrWhiteSpace(get_dashboard_body.payload.lat))
                    {
                        try
                        {
                            
                            string view_request_string = db_config.Get_Prefix_DB_Url($"mmrds/_design/sortable/_view/by_date_last_updated?skip=0&limit=30000&descending=true");
                            var case_view_curl = new cURL("GET", null, view_request_string, null, db_config.user_name, db_config.user_value);
                            string responseFromServer = await case_view_curl.executeAsync();



                            mmria.common.model.couchdb.case_view_response case_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.case_view_response>(responseFromServer);

                            
                            var data = case_view_response.rows
                                .Where
                                (
                                    cvi => cvi.value.record_id.Equals(post_payload.id, StringComparison.OrdinalIgnoreCase)
                                ).FirstOrDefault();

                            string case_request_string = db_config.Get_Prefix_DB_Url($"mmrds/{data.id}");


                            var case_curl = new cURL("GET", null, case_request_string, null, db_config.user_name, db_config.user_value);
                            string case_response = await case_curl.executeAsync();

                            var case_dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (case_response) as IDictionary<string,object>;

                            if
                            (
                                string.IsNullOrWhiteSpace(get_dashboard_body.payload.year) &&
                                case_dictionary != null &&
                                case_dictionary.ContainsKey("home_record")
                            )
                            {
                                var home_record = case_dictionary["home_record"] as IDictionary<string,object>;
                                if
                                (
                                    home_record != null &&
                                    home_record.ContainsKey("date_of_death")
                                )
                                {
                                     var date_of_death = home_record["date_of_death"] as IDictionary<string,object>;
                                     if
                                    (
                                        date_of_death != null &&
                                        date_of_death.ContainsKey("year") &&
                                        date_of_death["year"] != null
                                    )
                                    {
                                        get_dashboard_body.payload.year = date_of_death["year"].ToString();
                                    }
                                    else
                                    {
                                        file_status_result.is_valid_address = false;
                                    }

                                }
                                else
                                {
                                    if(string.IsNullOrWhiteSpace(get_dashboard_body.payload.year))
                                        file_status_result.is_valid_address = false;
                                }
                            }
                            else
                            {
                                if(string.IsNullOrWhiteSpace(get_dashboard_body.payload.year))
                                    file_status_result.is_valid_address = false;

                            }

                            if
                            (
                                string.IsNullOrWhiteSpace(get_dashboard_body.payload.lat) &&
                                case_dictionary != null &&
                                case_dictionary.ContainsKey("death_certificate")
                            )
                            {
                                var death_certificate = case_dictionary["death_certificate"] as IDictionary<string,object>;

                                if
                                (
                                    death_certificate != null &&
                                    death_certificate.ContainsKey("place_of_last_residence")
                                )
                                {
                                    var place_of_last_residence =  death_certificate["place_of_last_residence"] as IDictionary<string,object>;
                                    if
                                    (
                                        place_of_last_residence != null &&
                                        place_of_last_residence.ContainsKey("latitude") &&
                                        place_of_last_residence.ContainsKey("longitude") &&
                                        place_of_last_residence["latitude"] != null &&
                                        place_of_last_residence["longitude"] != null 
                                    )
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
                        catch(Exception ex)
                        {
                            Console.WriteLine (ex);
                        } 
            
                    }


                    if(!string.IsNullOrWhiteSpace(get_dashboard_body.payload.year))
                    {

                        int test_year = -1;
                        int selected_year = -1;
                        
                        if(int.TryParse(get_dashboard_body.payload.year, out test_year))
                        {
                            selected_year = test_year;

                            var get_year_body = new get_year_post_body()
                            {
                                id = cvs.cvs_api_id,
                                secret = cvs.cvs_api_key,
                                payload = new()
                            };

                            body_text = JsonSerializer.Serialize(get_year_body);
                            var get_year_curl = new cURL("POST", null, base_url, body_text, db_config.user_name, db_config.user_value);
                            string get_year_response = await get_year_curl.executeAsync();
                            var valid_year_list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>> (get_year_response);
                            if
                            (
                                valid_year_list != null &&
                                valid_year_list.Count > 0 &&
                                ! valid_year_list.Contains(selected_year)
                            )
                            {

                                var lower_diff = System.Math.Abs(valid_year_list[0] - selected_year);
                                var upper_diff = System.Math.Abs(valid_year_list[valid_year_list.Count -1] - selected_year);

                                if(lower_diff < upper_diff)
                                {
                                    if(lower_diff <= year_difference_limit)
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
                                    if(upper_diff <= year_difference_limit)
                                    {
                                        get_dashboard_body.payload.year = valid_year_list[valid_year_list.Count -1].ToString();
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


                    if
                    (
                        ! file_status_result.is_valid_address ||
                        ! file_status_result.is_valid_year
                    )
                    {
                        file_status_result.file_status = "Validation Error";
                        return Ok(file_status_result);
                    }

                    
                    if
                    (
                        string.IsNullOrWhiteSpace(get_dashboard_body.payload.year) ||
                        string.IsNullOrWhiteSpace(get_dashboard_body.payload.lat) ||
                        string.IsNullOrWhiteSpace(get_dashboard_body.payload.lon)
                    )
                    {
                        file_status_result.is_valid_address = false;
                    }





                    body_text = JsonSerializer.Serialize(get_dashboard_body);
                    var get_dashboard_curl = new mmria.server.cURL("POST", null, base_url, body_text);

                    response_string = await get_dashboard_curl.executeAsync();
                    System.Console.WriteLine(response_string);

                    responseDictionary = JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(response_string) as IDictionary<string,object>;


/*
"body": "\"PDF creation has been initiated and should be ready shortly. Please retry API call\""
"body": "\"PDF is being created!\""
"body": "JVBERi0xLjQKJazcIKu6CjEgMCBvYmoKPDwgL1BhZ2VzIDIgMCBSIC9UeXBlIC9DYXRhbG9nID4YXRlRGVjb2RlIC9MZW5 [TRUNCATED]",
"isBase64Encoded": true
*/
                    

                    if
                    (
                        responseDictionary != null 
                    )
                    {
                        

                        if
                        (
                            responseDictionary.ContainsKey("isBase64Encoded") &&
                            responseDictionary["isBase64Encoded"] != null &&
                            responseDictionary["isBase64Encoded"].ToString() == "True"
                        )
                        {
                            var bytes = Convert.FromBase64String(responseDictionary["body"].ToString());
                            var contents = new System.Net.Http.StreamContent(new MemoryStream(bytes));

                            var file_path = System.IO.Path.Combine(folder_name, $"CVS-{post_payload.id}.pdf");

                            System.IO.File.WriteAllBytes(file_path, bytes);

                            file_status_result.file_status = "file ready";

                        }
                        else if
                        (
                            responseDictionary.ContainsKey("body") &&
                            responseDictionary["body"].ToString().StartsWith("PDF ")
                        )
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
                    result = Ok(file_status_result);
                    
                    break;
            }
        }
        catch(System.Net.WebException ex)
        {
            System.Console.WriteLine($"cvsAPIController  POST\n{ex}");
            
            return Problem(
                type: "/docs/errors/forbidden",
                title: "CVS API Error",
                detail: ex.Message,
                statusCode: (int) ex.Status,
                instance: HttpContext.Request.Path
            );
        }


        if(result == null)
        {
            //return JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(response_string);
            return Ok(JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(response_string));
        }
        else
        {
            //return null;
            return result;
        }

        return result;
    }

    async Task<byte[]> GetFile(string s)
    {
        byte[] data;
        int br;
        int fs_length;

        using(FileStream fs = new FileStream (s, FileMode.Open, FileAccess.Read))
        {
            fs_length = (int) fs.Length;
            data = new byte[fs.Length];
            br = await fs.ReadAsync(data, 0, data.Length);
        }
        if (br != (int) fs_length)
            throw new System.IO.IOException(s);
        return data;
    }


} 


