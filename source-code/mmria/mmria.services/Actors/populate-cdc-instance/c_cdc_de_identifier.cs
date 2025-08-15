using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mmria.server.utils;

public sealed class c_cdc_de_identifier
{
    string case_item_json;

    string prefix = null;
    HashSet<string> de_identified_set = new HashSet<string>();
    
    common.couchdb.DBConfigurationDetail connection;
    string metadata_release_version_name;
    public c_cdc_de_identifier (string p_case_item_json, string p_prefix, common.couchdb.DBConfigurationDetail p_connection, string p_metadata_release_version_name)
    {
        this.case_item_json = p_case_item_json;
        this.prefix = p_prefix.ToLower();
        this.connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
    }
    public async Task<string> executeAsync()
    {
        string result = null;

        var de_identified_list_curl = new mmria.getset.cURL("GET", null, connection.url + "/metadata/de-identified-export-list", null, connection.user_name, connection.user_value);
        System.Dynamic.ExpandoObject de_identified_ExpandoObject = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(await de_identified_list_curl.executeAsync());
        IDictionary<string, object> idictionary = de_identified_ExpandoObject as IDictionary<string, object>;
        if(idictionary != null)
        {
            de_identified_set = new HashSet<string>();
            IDictionary<string, object> name_path_list = idictionary["name_path_list"] as IDictionary<string, object>;
            if(name_path_list != null)
            {
                var path_name = "global";

                if(name_path_list.ContainsKey(this.prefix))
                {
                    path_name = this.prefix;
                }
                
                foreach(string path in (IList<object>)name_path_list[path_name])
                {
                    de_identified_set.Add(path);
                }
            }
        }

        if(this.case_item_json == null || de_identified_set.Count == 0)
        {
            return result;
        }

        System.Dynamic.ExpandoObject case_item_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(case_item_json);


        IDictionary<string, object> expando_object = case_item_object as IDictionary<string, object>;

        if(expando_object != null)
        {
            expando_object.Remove("_rev");
        }
        else
        {
            return result;
        }

        bool is_fully_de_identified = true;
        try 
        {

            foreach (string path in de_identified_set) 
            {
                is_fully_de_identified  = is_fully_de_identified && set_de_identified_value (case_item_object, path);
                /*
                if(!is_fully_de_identified)
                {
                    set_de_identified_value (case_item_object, path);
                }*/
            }

            if(!is_fully_de_identified)
            {

                System.Console.WriteLine ("Not fully de-identified");

                string de_identified_json;

                string current_directory = AppContext.BaseDirectory;
                if(!System.IO.Directory.Exists(System.IO.Path.Combine(current_directory, "database-scripts")))
                {
                    current_directory = System.IO.Directory.GetCurrentDirectory();
                }

                using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine( current_directory,  $"database-scripts/case-version-{metadata_release_version_name}.json")))
                {
                    de_identified_json = sr.ReadToEnd();
                }

                var case_expando_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (de_identified_json);


                var byName = (IDictionary<string,object>)case_expando_object;
                var created_by = byName["created_by"] as string;
                if(string.IsNullOrWhiteSpace(created_by))
                {
                    byName["created_by"] = "system";
                } 

                if(byName.ContainsKey("last_updated_by"))
                {
                    byName["last_updated_by"] = "system";
                }
                else
                {
                    byName.Add("last_updated_by", "system");
                    
                }

                byName["_id"] = expando_object["_id"]; 
                

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                result = Newtonsoft.Json.JsonConvert.SerializeObject(case_expando_object, settings);
            }
            else
            {
                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                result = Newtonsoft.Json.JsonConvert.SerializeObject(case_item_object, settings);

            } 


        }
        catch (Exception ex) 
        {
            System.Console.WriteLine ($"de-identify exception {ex}");
        }

        return result;
    }


    public bool set_de_identified_value (dynamic p_object, string p_path)
    {

        bool result = false;
        /*
        if (p_path == "geocode_quality_indicator")
        {
            System.Console.Write("break");
        }*/

        try
        {
            ///"death_certificate/place_of_last_residence/street",

            List<string> path_list = new List<string>(p_path.Split ('/'));

            if (path_list.Count == 1)
            {	
                if (p_object is IDictionary<string, object>)
                {
                    
                    IDictionary<string, object> dictionary_object = p_object as IDictionary<string, object>;

                    object val = null;

                    if (dictionary_object.ContainsKey (path_list [0]))
                    {
                        val = dictionary_object [path_list [0]]; 

                        if (val != null)
                        {
                            // set the de-identified value
                            if (val is IDictionary<string, object>)
                            {
                                //System.Console.WriteLine ("This should not happen. {0}", p_path);
                            }
                            else if (val is IList<object>)
                            {
                                //System.Console.WriteLine ("This should not happen. {0}", p_path);
                            }
                            else if (val is string)
                            {
                                //dictionary_object [path_list [0]] = "de-identified";
                                if(
                                    path_list [0] == "first_name" ||
                                    path_list [0] == "last_name"
                                )
                                {
                                    dictionary_object [path_list [0]] = "de-identified";
                                    result = true;
                                }
                                else
                                {
                                    dictionary_object [path_list [0]] = null;
                                    result = true;
                                }
                            }
                            else if (val is System.DateTime)
                            {
                                //dictionary_object [path_list [0]] = DateTime.MinValue;
                                dictionary_object [path_list [0]] = null;
                                result = true;
                            }
                            else
                            {
                                dictionary_object [path_list [0]] = null;
                                result = true;
                            }
                        }
                        else
                        {
                            result = true;
                        }
                    }
                    else
                    {
                        result = true;
                    }
            
                }
                else if (p_object is IList<object>)
                {
                    IList<object> Items = p_object as IList<object>;

                    if(Items.Count > 0)
                    {
                        foreach(object item in Items)
                        {
                            result = set_de_identified_value (item, path_list [0]);

                        }
                    }
                    else
                    {
                        result = true;
                    }
                }	
                else
                {
                    //System.Console.WriteLine ("This should not happen. {0}", p_path);
                    result = false;
                }
                
            }
            else
            {
                List<string> new_path = new List<string>();

                for(int i = 1; i < path_list.Count; i++)
                {
                    new_path.Add(path_list[i]);
                }
                // call set_de_identified_value with next item in path
                ///"death_certificate/place_of_last_residence/street",
                //er_visit_and_hospital_medical_records/basic_admission_and_discharge_information/date_of_arrival/day

                if (p_object is IDictionary<string, object>)
                {
                    IDictionary<string, object> dictionary_object = p_object as IDictionary<string, object>;

                    object val = null;

                    if (dictionary_object.ContainsKey (path_list [0]))
                    {
                        val = dictionary_object [path_list [0]]; 
                    }

                    if (val != null)
                    {

                        result = set_de_identified_value (val, string.Join("/", new_path));
                    }
                    else
                    {
                        result = true;
                    }

                }
                else if (p_object is IList<object>)
                {
                    
                    IList<object> Items = p_object as IList<object>;

                    if(Items.Count > 0)
                    {
                        foreach(object item in Items)
                        {
                            result = set_de_identified_value (item, string.Join("/", path_list));

                        }
                    }
                    else
                    {
                        result = true;
                    }

                }
                else
                {
                    //System.Console.WriteLine ("This should not happen. {0}", p_path);
                    result = false;
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine ("set_de_identified_value. {0}", ex);
            result = false;
        }
        
        return result;
    }
}


