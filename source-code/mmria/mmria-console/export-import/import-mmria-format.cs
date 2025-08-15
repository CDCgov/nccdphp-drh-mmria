using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Threading.Tasks;

namespace mmria.console;

public sealed class import_mmria_format
{
    private string auth_token = null;
    private string user_name = null;
    private string password = null;
    private string source_file_path = null;
    private string mmria_url = null;

    //import user_name:user1 password:password database_file_path:mapping-file-set/Maternal_Mortality.mdb url:http://localhost:12345

    public import_mmria_format()
    {
        

    }
    public async Task Execute(string[] args)
    {

        if (args.Length > 1)
        {
            for (var i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                int index = arg.IndexOf(':');
                string val = arg.Substring(index + 1, arg.Length - (index + 1)).Trim(new char[] { '\"' });

                if (arg.ToLower().StartsWith("auth_token"))
                {
                    this.auth_token = val;
                }
                else if (arg.ToLower().StartsWith("user_name"))
                {
                    this.user_name = val;
                }
                else if (arg.ToLower().StartsWith("password"))
                {
                    this.password = val;
                }
                else if (arg.ToLower().StartsWith("source_file_path"))
                {
                    
                    this.source_file_path = val;
                }
                else if (arg.ToLower().StartsWith("url"))
                {
                    this.mmria_url = val;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(this.source_file_path))
        {
            System.Console.WriteLine("missing source_file_path");
            System.Console.WriteLine(" form source_file_path:[file path]");
            System.Console.WriteLine(" example 1 source_file_path:c:/temp/");
            System.Console.WriteLine(" example 2 source_file_path:\"c:/temp folder/\"");
            System.Console.WriteLine(" example 3 source_file_path:mapping-file-set\\\"");
            System.Console.WriteLine(" mmria.exe import user_name:user1 password:secret url:http://localhost:12345 source_file_path:\"c:\\temp folder\\\"");

            return;
        }

        if (string.IsNullOrWhiteSpace(this.mmria_url))
        {
            System.Console.WriteLine("missing url");
            System.Console.WriteLine(" form url:[website_url]");
            System.Console.WriteLine(" example url:http://localhost:12345");

            return;
        }

        if (string.IsNullOrWhiteSpace(this.user_name))
        {
            System.Console.WriteLine("missing user_name");
            System.Console.WriteLine(" form user_name:[user_name]");
            System.Console.WriteLine(" example user_name:user1");
            return;
        }

        if (string.IsNullOrWhiteSpace(this.password))
        {
            System.Console.WriteLine("missing password");
            System.Console.WriteLine(" form password:[password]");
            System.Console.WriteLine(" example password:secret");
            return;
        }



        if (!Directory.Exists(this.source_file_path))
        {
            System.Console.WriteLine($"source_file_path does NOT exist: {this.source_file_path}");
            return;
        }


        string login_url = this.mmria_url + "/api/session/";
        var login_data = new { userid = this.user_name, password = this.password };
        var login_curl = new cURL("POST", null, login_url, Newtonsoft.Json.JsonConvert.SerializeObject(login_data));
        string login_result_string = null;
        try
        {
            login_result_string = await login_curl.executeAsync();
        }
        catch(Exception ex)
        {
            System.Console.WriteLine(ex);
        }
        
        var login_result_data = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<System.Dynamic.ExpandoObject>>(login_result_string);
        IDictionary<string, object> login_result_dictionary = login_result_data[0] as IDictionary<string, object>;

        string auth_session = null;

        //{"ok":true,"userCtx":{"name":null,"roles":[]},"info":{"authentication_db":"_users","authentication_handlers":["oauth","cookie","default"]}}
        if (login_result_dictionary == null || !login_result_dictionary.ContainsKey ("ok") && login_result_dictionary["ok"].ToString().ToLower() != "true") 
        {
            System.Console.WriteLine($"Unable to login to {this.mmria_url}:\n{login_result_dictionary}");
            return;
        }


        auth_session = login_result_dictionary ["auth_session"].ToString();



        foreach (var file_name in System.IO.Directory.GetFiles(this.source_file_path))
        {
            

            string global_record_id = null;

            string case_json_string = System.IO.File.ReadAllText(file_name);

            var case_data = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(case_json_string);
            IDictionary<string, object> case_data_dictionary = case_data as IDictionary<string, object>;

            if(case_data_dictionary == null)
            {
                continue;
            }

            if (case_data_dictionary.ContainsKey ("_id")) 
            {
                global_record_id = case_data_dictionary ["_id"].ToString();
            }


            if (case_data_dictionary.ContainsKey ("_rev")) 
            {
                case_data_dictionary.Remove("_rev");
            }

            try
            {
                // check if doc exists
                string document_url = this.mmria_url + "/api/case?case_id=" + global_record_id;
                var document_curl = new cURL("GET", null, document_url, null, null, null);
                document_curl.AddCookie("AuthSession", auth_session);
                string document_json = null;

                document_json =  document_curl.execute();
                var result_expando = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(document_json);
                var result = result_expando as IDictionary<string, object>;
                if (result != null && result.ContainsKey ("_rev")) 
                {
                    case_data_dictionary ["_rev"] = result ["_rev"];


                    //System.Console.WriteLine ("json\n{0}", case_json_string);
                }

                
                case_json_string = Newtonsoft.Json.JsonConvert.SerializeObject (case_data, new Newtonsoft.Json.JsonSerializerSettings () {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc
                });

                var update_curl = new cURL ("POST", null, this.mmria_url + "/api/case", case_json_string, null, null);
                update_curl.AddCookie("AuthSession", auth_session);
                try 
                {
                    string update_result_string = update_curl.execute ();

                    /*
                    var update_result_expando = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(update_result_string);
                    var update_result = result_expando as IDictionary<string, object>;
                    if (update_result != null && update_result.ContainsKey ("_rev")) 
                    {
                        
                        case_data_dictionary ["_rev"] = result ["_rev"];


                        case_json_string = Newtonsoft.Json.JsonConvert.SerializeObject (case_data, new Newtonsoft.Json.JsonSerializerSettings () {
                            Formatting = Newtonsoft.Json.Formatting.Indented,
                            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
                            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc
                        });
                        System.Console.WriteLine ("json\n{0}", case_json_string);
                        
                    }
                    */
                    System.Console.WriteLine ($"Succesfully imported id {global_record_id}");
                    //System.Console.WriteLine (update_result_string);

                }
                catch (Exception ex) 
                {
                    System.Console.WriteLine ($"Id {global_record_id}");
                    System.Console.WriteLine (ex);
                }

            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Get case");
                System.Console.WriteLine(ex);
                System.Console.WriteLine("json\n{0}", case_json_string);

            }
        }


        Console.WriteLine("Import Finished");



    }
}

