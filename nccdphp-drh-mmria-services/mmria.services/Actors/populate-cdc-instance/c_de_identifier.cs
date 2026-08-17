using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mmria.common.SharedLibraries.MetadataVersion;
using mmria.common.SharedLibraries.MetadataVersion.DAL;

namespace mmria.server.utils;

public sealed class c_de_identifier
{
    string case_item_json;

    common.couchdb.DBConfigurationDetail connection;

    string metadata_release_version_name;
    HashSet<string> de_identified_set = new HashSet<string>();
    mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly IMetadataRepository _metadataRepository;
    private readonly System.Dynamic.ExpandoObject _case_item_object;
    private readonly c_document_sync_rebuild_context _rebuild_context;
    
    public c_de_identifier
    (
        string p_case_item_json,
        common.couchdb.DBConfigurationDetail p_connection,
        string p_metadata_release_version_name,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        System.Dynamic.ExpandoObject p_case_item_object = null,
        c_document_sync_rebuild_context p_rebuild_context = null
    )
    {
        this.case_item_json = p_case_item_json;
        connection = p_connection;
        metadata_release_version_name = p_metadata_release_version_name;
        _couchDbHttpClient = couchDbHttpClient;
        _metadataRepository = new MetadataVersionDAL(couchDbHttpClient);
        _case_item_object = p_case_item_object;
        _rebuild_context = p_rebuild_context;
    }
    public async Task<string> executeAsync()
    {
        string result = null;

        if(_rebuild_context?.de_identified_set?.Count > 0)
        {
            de_identified_set = new HashSet<string>(_rebuild_context.de_identified_set, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            System.Dynamic.ExpandoObject de_identified_ExpandoObject = await _metadataRepository.GetDeIdentifiedListAsync(connection);
            de_identified_set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(string path in (IList<object>)(((IDictionary<string, object>)de_identified_ExpandoObject)["paths"]))
            {
                de_identified_set.Add(path);
            }
        }


        if(this.case_item_json == null || de_identified_set.Count == 0)
        {
            return result;
        }

        System.Dynamic.ExpandoObject case_item_object = _case_item_object != null
            ? clone_expando_object(_case_item_object)
            : Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(case_item_json);


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
                bool path_result = set_de_identified_value(case_item_object, path);
                if(!path_result)
                {
                    var case_id = (expando_object.ContainsKey("_id") ? expando_object["_id"]?.ToString() : null) ?? "unknown";
                    System.Console.WriteLine($"[DeIdDiag] [case:{case_id}] path returned false: {path}");
                }
                is_fully_de_identified = is_fully_de_identified && path_result;
            }

            if(!is_fully_de_identified)
            {

                System.Console.WriteLine ("Not fully de-identified");

                string de_identified_json = _rebuild_context != null
                    ? _rebuild_context.case_template_json
                    : null;

                if(string.IsNullOrWhiteSpace(de_identified_json))
                {
                    var case_template_path = mmria.common.SharedLibraries.MMRIAServices.Helper.MMRIAServicesHelper.ResolveDatabaseScriptPath($"case-version-{metadata_release_version_name}.json");

                    using (var sr = new System.IO.StreamReader(case_template_path))
                    {
                        de_identified_json = sr.ReadToEnd();
                    }
                }

                if(string.IsNullOrWhiteSpace(de_identified_json))
                {
                    return result;
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

    private static System.Dynamic.ExpandoObject clone_expando_object(System.Dynamic.ExpandoObject source)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(source);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(json);
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


