using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading.Tasks;

namespace mmria.console.replicate;

public sealed class Replicate
{
    public Replicate ()	{		}
    public async Task Execute (string [] args)
    {
        string source_db = "fl";

        string[] target_list = {
            "id",
            "ky",
            "mo",
            "ms",
            "nv",
            "ny",
            "or",
            "wa"
        };

        var id= "";
        var value= "";

        foreach( var item in target_list)
        {
            
            string replicate_url = $"https://couchdb-{item}-mmria.services.cdc.gov/_replicate";

            var data = new Replication_Message()
            {
                source = $"https://couchdb-{source_db}-mmria.services.cdc.gov/metadata",
                target = $"https://couchdb-{item}-mmria.services.cdc.gov/metadata"

            };

            try 
            {
                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string repolication_message_string = Newtonsoft.Json.JsonConvert.SerializeObject (data, settings);

                cURL document_curl = new cURL ("POST", null, replicate_url, repolication_message_string, id, value);
                var curl_result = await document_curl.executeAsync();

                Console.WriteLine (curl_result);
            }
            catch (Exception ex) 
            {
                Console.WriteLine (ex);
            }
        }

        Console.WriteLine ("Replication Finished.");
    }


}

