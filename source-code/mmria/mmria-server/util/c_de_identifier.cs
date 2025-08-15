using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace mmria.server.utils;

public sealed class c_de_identifier
{
    string case_item_json;
    string metadata_version;
    mmria.common.couchdb.DBConfigurationDetail db_config = null;
    HashSet<string> de_identified_set = new HashSet<string>();
    HashSet<string> date_offset_set = new HashSet<string>()
    {
        "prenatal/routine_monitoring/date_and_time",
        "er_visit_and_hospital_medical_records/vital_signs/date_and_time"
    };
    int date_offset_days;
    
    public c_de_identifier 
    (
        string p_case_item_json,
        string p_metadata_version,
        mmria.common.couchdb.DBConfigurationDetail _db_config
    )
    {
        this.case_item_json = p_case_item_json;
        metadata_version = p_metadata_version;
        db_config = _db_config;

        var CprytoRNG = new System.Security.Cryptography.RNGCryptoServiceProvider();


        int RandomIntFromRNG(int min, int max)
        {

            byte[] four_bytes = new byte[4];
            CprytoRNG.GetBytes(four_bytes);


            UInt32 scale = BitConverter.ToUInt32(four_bytes, 0);

            return (int)(min + (max - min) * (scale / (uint.MaxValue + 1.0)));
        }

        date_offset_days = -1 * RandomIntFromRNG(20000, 20101);



    }



    public async Task<string> executeAsync()
    {
        string result = null;

        cURL de_identified_list_curl = new cURL("GET", null, db_config.url + "/metadata/de-identified-list", null, db_config.user_name, db_config.user_value);
        System.Dynamic.ExpandoObject de_identified_ExpandoObject = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(await de_identified_list_curl.executeAsync());
        de_identified_set = new HashSet<string>();
        foreach(string path in (IList<object>)(((IDictionary<string, object>)de_identified_ExpandoObject) ["paths"]))
        {
            de_identified_set.Add(path);
        }

        // cURL date_offset_list_curl = new cURL("GET", null, db_config.url + "/metadata/date-offset-list", null, db_config.user_name, db_config.user_value);
        // System.Dynamic.ExpandoObject date_offset_ExpandoObject = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(await date_offset_list_curl.executeAsync());
        // date_offset_set = new HashSet<string>();
        // foreach(string path in (IList<object>)(((IDictionary<string, object>)date_offset_ExpandoObject) ["paths"]))
        // {
        //     date_offset_set.Add(path);
        // }

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

                is_fully_de_identified  = is_fully_de_identified && set_de_identified_value (case_item_object, path, path.AsSpan());
                
               
                if(!is_fully_de_identified)
                {
                    break;
                    //System.Console.WriteLine("here");
                }
                 /*  */
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

                using (var  sr = new System.IO.StreamReader(System.IO.Path.Combine( current_directory,  $"database-scripts/case-version-{metadata_version}.json")))
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


    public bool set_de_identified_value (dynamic p_object, string p_path, ReadOnlySpan<char> full_path)
    {
        bool result = false;

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
                                else if(date_offset_set.Contains(full_path.ToString()))
                                {
                                    if(!string.IsNullOrWhiteSpace(val.ToString()))
                                    {
                                        var val_string = val.ToString();

                                        if(val_string.Contains("-"))
                                        {
                                            var space_split = val_string.Split(" ");
                                            var date_arr = space_split[0].ToString().Split("-");
                                            var date = new DateOnly
                                            (
                                                int.Parse(date_arr[0]),
                                                int.Parse(date_arr[1]),
                                                int.Parse(date_arr[2])
                                            );
                                        

                                            dictionary_object [path_list [0]] = date.AddDays(date_offset_days);
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
                                    dictionary_object [path_list [0]] = null;
                                    result = true;
                                }
                            }
                            else if (val is System.DateTime date_time_val)
                            {
                                //System.Console.WriteLine("found a date: {0}", p_path);

                                if(date_offset_set.Contains(full_path.ToString()))
                                {
                                    dictionary_object [path_list [0]] = date_time_val.AddDays(date_offset_days);
                                    result = true;
                                }
                                else
                                {
                                    //dictionary_object [path_list [0]] = DateTime.MinValue;
                                    // if (dictionary_object.ContainsKey(p_path[0]))
                                    //   dictionary_object [path_list [0]] = dictionary_object.TryGetValue() + (-dateoffset);
                                    // else
                                    dictionary_object [path_list [0]] = null;
                                    result = true;
                                }
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
                            result = set_de_identified_value (item, path_list [0], full_path);
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

                        result = set_de_identified_value (val, string.Join("/", new_path), full_path);
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
                            result = set_de_identified_value (item, string.Join("/", path_list), full_path);

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


