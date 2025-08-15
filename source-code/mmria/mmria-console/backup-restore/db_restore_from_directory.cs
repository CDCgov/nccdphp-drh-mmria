using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace mmria.console.db;

public sealed class Restore_From_Directory
{
    private string auth_token = null;
    private string user_name = null;
    private string user_value = null;
    private string backup_file_path = null;
    private string database_url = null;
    private bool is_offline_mode = false;

    //import user_name:user1 password:password database_file_path:mapping-file-set/Maternal_Mortality.mdb url:http://localhost:12345

    public Restore_From_Directory()
    {
        

    }
    public async Task Execute(string[] args)
    {
        string import_directory = null; //System.Configuration.ConfigurationManager.AppSettings["import_directory"];
        this.is_offline_mode = false;  //bool.Parse(System.Configuration.ConfigurationManager.AppSettings["is_offline_mode"]);




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
                    this.user_value = val;
                }
                else if (arg.ToLower().StartsWith("backup_file_path"))
                {
                    
                    this.backup_file_path = val;
                }
                else if (arg.ToLower().StartsWith("database_url"))
                {
                    this.database_url = val;
                }
                else if(arg.ToLower().StartsWith("import_directory"))
                {
                    import_directory = val;
                }
                else if(arg.ToLower().StartsWith("is_offline_mode"))
                {
                    is_offline_mode = bool.Parse(val);
                }
            }
        }
/*
        if (!System.IO.Directory.Exists(import_directory))
        {
            System.IO.Directory.CreateDirectory(import_directory);
        }
*/


        if (string.IsNullOrWhiteSpace(this.backup_file_path) && string.IsNullOrWhiteSpace(import_directory))
        {
            System.Console.WriteLine("missing backup_file_path");
            System.Console.WriteLine(" form backup_file_path:[file path]");
            System.Console.WriteLine(" example backup_file_path:c:\\temp\\2017-01-01.bk");
            System.Console.WriteLine(" mmria.exe restore user_name:user1 password:secret url:http://localhost:12345 database_file_path:\"c:\\temp folder\\maternal_mortality.mdb\"");

            return;
        }

        if (string.IsNullOrWhiteSpace(this.database_url))
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

        if (string.IsNullOrWhiteSpace(this.user_value))
        {
            System.Console.WriteLine("missing password");
            System.Console.WriteLine(" form password:[password]");
            System.Console.WriteLine(" example password:secret");
            return;
        }


        if(import_directory != null)
        {
            foreach (string file in System.IO.Directory.EnumerateFiles(import_directory, "*.json"))
            {
                string json_string = System.IO.File.ReadAllText(file);
                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                var doc_Expando =
                Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (json_string, settings);

                var doc = doc_Expando as IDictionary<string,object>;

                if (doc.ContainsKey ("_rev")) 
                {
                    doc.Remove("_rev");
                }

                if (doc.ContainsKey ("_attachments")) 
                {
                    doc.Remove("_attachments");
                }


                string _id = "";

                if (doc.ContainsKey ("_id")) 
                {
                    _id = doc ["_id"].ToString();
                }
                else
                {
                    continue;
                }

                if (_id.IndexOf ("_design/") > -1)
                {
                    continue;
                }
                else if(doc.ContainsKey("home_record"))
                {
                    if(doc["home_record"] is IDictionary<string,object>)
                    {
                        var home_record = doc["home_record"] as IDictionary<string,object>;

                        if(!home_record.ContainsKey("jurisdiction_id"))
                        {
                            home_record.Add("jurisdiction_id", "/");
                        }
                        else if(home_record["jurisdiction_id"] == null)
                        {
                            home_record["jurisdiction_id"] = "/";
                        }
                        else if(string.IsNullOrWhiteSpace(home_record["jurisdiction_id"].ToString()))
                        {
                            home_record["jurisdiction_id"] = "/";
                        }/**/
                    }
                    else if(doc["home_record"] is Newtonsoft.Json.Linq.JObject)
                    {
                        var home_record = doc["home_record"] as Newtonsoft.Json.Linq.JObject;

                        if(!home_record.ContainsKey("jurisdiction_id"))
                        {
                            home_record.Add("jurisdiction_id", "/");
                        }
                        else if(home_record["jurisdiction_id"] == null)
                        {
                            home_record["jurisdiction_id"] = "/";
                        }
                        else if(string.IsNullOrWhiteSpace(home_record["jurisdiction_id"].ToString()))
                        {
                            home_record["jurisdiction_id"] = "/";
                        }/**/
                    }
                }


                try
                {
                
                    string put_result = await Put_Document (doc, _id);

                    Console.WriteLine (put_result);
                }
                catch(System.Exception ex)
                {
                    Console.WriteLine ("Error: " + _id);
                    Console.WriteLine(ex);
                }


            }
        }
        else if (System.IO.File.Exists (this.backup_file_path)) 
        {
            string bulk_document_string = System.IO.File.ReadAllText (this.backup_file_path);
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            mmria.console.model.couchdb.cBulkDocument bulk_document =
                Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.console.model.couchdb.cBulkDocument> (bulk_document_string, settings);

            
            foreach(var doc in bulk_document.docs)
            {
                
                
                if (doc.ContainsKey ("_rev")) 
                {
                    doc.Remove("_rev");
                }

                if (doc.ContainsKey ("_attachments")) 
                {
                    doc.Remove("_attachments");
                }


                string _id = "";

                if (doc.ContainsKey ("_id")) 
                {
                    _id = doc ["_id"].ToString();
                }
                else
                {
                    continue;
                }

                if (_id.IndexOf ("_design/") > -1)
                {
                    continue;
                }
                else if(doc.ContainsKey("home_record"))
                {
                    if(doc["home_record"] is IDictionary<string,object>)
                    {
                        var home_record = doc["home_record"] as IDictionary<string,object>;

                        if(!home_record.ContainsKey("jurisdiction_id"))
                        {
                            home_record.Add("jurisdiction_id", "/");
                        }
                        else if(home_record["jurisdiction_id"] == null)
                        {
                            home_record["jurisdiction_id"] = "/";
                        }
                        else if(string.IsNullOrWhiteSpace(home_record["jurisdiction_id"].ToString()))
                        {
                            home_record["jurisdiction_id"] = "/";
                        }/**/
                    }
                    else if(doc["home_record"] is Newtonsoft.Json.Linq.JObject)
                    {
                        var home_record = doc["home_record"] as Newtonsoft.Json.Linq.JObject;

                        if(!home_record.ContainsKey("jurisdiction_id"))
                        {
                            home_record.Add("jurisdiction_id", "/");
                        }
                        else if(home_record["jurisdiction_id"] == null)
                        {
                            home_record["jurisdiction_id"] = "/";
                        }
                        else if(string.IsNullOrWhiteSpace(home_record["jurisdiction_id"].ToString()))
                        {
                            home_record["jurisdiction_id"] = "/";
                        }/**/
                    }
                }



                
                string put_result = await Put_Document (doc, _id);

                Console.WriteLine (put_result);
            } 
            /**/
/*
            string post_result = await Post_Document_List (bulk_document);

            Console.WriteLine (post_result);
            */

            Console.WriteLine ("Restore Finished.");
        }
        else 
        {
            Console.WriteLine ("Backup File does NOT exist. {0}", this.backup_file_path);
        }

    }



    private async Task<string> Put_Document (IDictionary<string,object> p_value, string p_id)
    {

        string result = null;
        string document_json = Newtonsoft.Json.JsonConvert.SerializeObject(p_value);
        string URL = $"{this.database_url}/{p_id}";
        cURL document_curl = new cURL ("PUT", null, URL, document_json, this.user_name, this.user_value);
        try
        {
            result = await document_curl.executeAsync ();
        }
        catch (Exception ex)
        {
            result = ex.ToString ();
        }
        return result;
    }

    private async Task<string> Post_Document_List (mmria.console.model.couchdb.cBulkDocument p_bulk_document)
    {

        string result = null;
        string bulk_document_string = Newtonsoft.Json.JsonConvert.SerializeObject(p_bulk_document);
        string URL = string.Format ("{0}/_bulk_docs", this.database_url);
        cURL document_curl = new cURL ("POST", null, URL, bulk_document_string, this.user_name, this.user_value);
        try
        {
            result = await document_curl.executeAsync ();
        }
        catch (Exception ex)
        {
            result = ex.ToString ();
        }
        return result;
    }

}

