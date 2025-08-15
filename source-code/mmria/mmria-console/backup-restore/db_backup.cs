using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading.Tasks;

namespace mmria.console.db;

public sealed class Backup
{
    private string auth_token = null;
    private string user_name = null;
    private string user_value = null;
    private string backup_file_path = null;
    private string database_url = null;
    private string mmria_url = null;

    public Backup ()
    {
    
        

    }
    public async Task Execute (string [] args)
    {
        string export_directory = null;





        if (args.Length > 1) 
        {
            for (var i = 1; i < args.Length; i++) 
            {
                string arg = args [i];
                int index = arg.IndexOf (':');
                string val = arg.Substring (index + 1, arg.Length - (index + 1)).Trim (new char [] { '\"' });

                if (arg.ToLower ().StartsWith ("auth_token")) 
                {
                    this.auth_token = val;
                } 
                else if (arg.ToLower ().StartsWith ("user_name"))
                {
                    this.user_name = val;
                }
                else if (arg.ToLower ().StartsWith ("password")) 
                {
                    this.user_value = val;
                }
                else if (arg.ToLower ().StartsWith ("database_url"))
                {
                    this.database_url = val;
                }
                else if (arg.ToLower ().StartsWith ("backup_file_path"))
                {
                    this.backup_file_path = val;
                }
                else if (arg.ToLower ().StartsWith ("url"))
                {
                    this.mmria_url = val;
                }
                else if(arg.ToLower().StartsWith("export_directory"))
                {
                    export_directory = val;
                }
            }
        }
/*
        if (!System.IO.Directory.Exists (export_directory)) 
        {
            System.IO.Directory.CreateDirectory (export_directory);
        }
*/
        if (string.IsNullOrWhiteSpace (this.database_url)) 
        {
            System.Console.WriteLine ("missing database_url");
            System.Console.WriteLine (" form backup_file_path:[file path]");
            System.Console.WriteLine (" example database:http://localhost:5984/metadata");
            System.Console.WriteLine (" mmria.exe backup user_name:user1 password:secret url:http://localhost:12345 database_url:http://localhost:5984/database_name");

            return;
        }

        if (string.IsNullOrWhiteSpace (this.user_name)) 
        {
            System.Console.WriteLine ("missing user_name");
            System.Console.WriteLine (" form user_name:[user_name]");
            System.Console.WriteLine (" example user_name:user1");
            System.Console.WriteLine (" mmria.exe export user_name:user1 password:secret url:http://localhost:12345");
            return;
        }

        if (string.IsNullOrWhiteSpace (this.user_value)) 
        {
            System.Console.WriteLine ("missing password");
            System.Console.WriteLine (" form password:[password]");
            System.Console.WriteLine (" example password:secret");
            System.Console.WriteLine (" mmria.exe export user_name:user1 password:secret url:http://localhost:12345");
            return;
        }

        /*
        var mmria_server = new mmria_server_api_client(this.mmria_url);
        mmria_server.login(this.user_name, this.password);

        mmria.common.metadata.app metadata = mmria_server.get_metadata();
        object all_cases = mmria_server.get_all_cases(this.database_url);
        */

        try 
        {
            mmria.console.model.couchdb.cBulkDocument bulk_document = await GetDocumentList ();

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string bulk_document_string = Newtonsoft.Json.JsonConvert.SerializeObject (bulk_document, settings);
            if (!System.IO.File.Exists (this.backup_file_path)) 
            {
                System.IO.File.WriteAllText (this.backup_file_path, bulk_document_string);
            }

            Console.WriteLine ("Backup Finished.");
        }
        catch (Exception ex) 
        {
            Console.WriteLine (ex);
        }

    }



    private async Task<mmria.console.model.couchdb.cBulkDocument> GetDocumentList ()
    {

        mmria.console.model.couchdb.cBulkDocument result = new model.couchdb.cBulkDocument ();

        string URL = string.Format("{0}/_all_docs?include_docs=true", this.database_url);
        cURL document_curl = new cURL ("GET", null, URL, null, this.user_name, this.user_value);
        var curl_result = await document_curl.executeAsync();

        object all_cases = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (curl_result);
        /*
        object all_cases_rows = all_cases.rows;

        foreach (System.Dynamic.ExpandoObject case_row in all_cases_rows) 
        {

            IDictionary<string, object> case_doc = ((IDictionary<string, object>)case_row) ["doc"] as IDictionary<string, object>;

            if(case_doc != null)
            {
                case_doc.Remove("_rev");
                result.docs.Add (case_doc);
            }
        }*/

        return result;
    }


}

