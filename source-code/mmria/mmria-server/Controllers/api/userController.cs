using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using mmria.common.model;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension;
namespace mmria.server;


[Route("api/[controller]")]
public sealed class userController: ControllerBase 
{ 

    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;

    IHttpContextAccessor httpContextAccessor;
    string host_prefix = null;

    public userController
	(
        IHttpContextAccessor p_httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {
        httpContextAccessor = p_httpContextAccessor; 
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }
    
    [Authorize(Roles  = "abstractor,data_analyst")]
    [Route("my-user")]
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.user> GetMyUser() 
    { 
        try
        {

            var User = httpContextAccessor.HttpContext.User;

            var userName = "";

            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == ClaimTypes.Name)).FindFirst(ClaimTypes.Name).Value;
            }

            string request_string = $"{db_config.url}/_users/org.couchdb.user:{userName}";

            var user_curl = new cURL("GET",null,request_string,null, db_config.user_name, db_config.user_value);
            string responseFromServer = await user_curl.executeAsync();

            var result  = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user>(responseFromServer);


            return result;

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
        
    } 

    //public IEnumerable<mmria.common.model.couchdb.user_alldocs_response> Get() 
    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpGet]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.get_response_header<mmria.common.model.couchdb.user>> Get(int skip = 1, int take = 9000) 
    { 

        try
        {
            var User = httpContextAccessor.HttpContext.User;

            #if !IS_PMSS_ENHANCED
            var jurisdiction_hashset = mmria.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, User);

            var jurisdiction_username_hashset = mmria.server.utils.authorization_case.get_user_jurisdiction_set(db_config);
            #endif
            #if IS_PMSS_ENHANCED
            var jurisdiction_hashset = mmria.pmss.server.utils.authorization.get_current_jurisdiction_id_set_for(db_config, User);

            var jurisdiction_username_hashset = mmria.pmss.server.utils.authorization_case.get_user_jurisdiction_set(db_config);
            #endif

            string request_string = $"{db_config.url}/_users/_all_docs?include_docs=true&skip={skip}&limit={take}";

            var user_curl = new cURL("GET",null,request_string,null, db_config.user_name, db_config.user_value);
            string responseFromServer = await user_curl.executeAsync();

            var user_alldocs_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_response_header<mmria.common.model.couchdb.user>>(responseFromServer);
        

            mmria.common.model.couchdb.get_response_header<mmria.common.model.couchdb.user> result = new mmria.common.model.couchdb.get_response_header<mmria.common.model.couchdb.user>();
            result.offset = user_alldocs_response.offset;
            result.total_rows = user_alldocs_response.total_rows;

            

            List<mmria.common.model.couchdb.get_response_item<mmria.common.model.couchdb.user>> temp_list = new List<mmria.common.model.couchdb.get_response_item<mmria.common.model.couchdb.user>>();
            foreach(mmria.common.model.couchdb.get_response_item<mmria.common.model.couchdb.user> uai in user_alldocs_response.rows)
            {
                bool is_jurisdiction_ok = false;
                bool is_app_prefix_ok = false;
                foreach(var jurisdiction_item in jurisdiction_hashset)
                {

                    if(string.IsNullOrWhiteSpace(db_config.prefix))
                    {
                        if(uai.doc.app_prefix_list == null || uai.doc.app_prefix_list.Count == 0)
                        {
                            is_app_prefix_ok = true;
                        }
                        else if(uai.doc.app_prefix_list.ContainsKey("__no_prefix__"))
                        {
                            is_app_prefix_ok = true;
                        }
                    }
                    else if(uai.doc.app_prefix_list.ContainsKey(db_config.prefix))
                    {
                        is_app_prefix_ok = uai.doc.app_prefix_list[db_config.prefix];
                    }

                    if(jurisdiction_item.jurisdiction_id == "/")
                    {
                        is_jurisdiction_ok = true;
                        break;
                    }
                    var regex = new System.Text.RegularExpressions.Regex("^" + @jurisdiction_item.jurisdiction_id);

                    foreach(var jurisdiction_username in jurisdiction_username_hashset)
                    {

                        if
                        (
                            regex.IsMatch(jurisdiction_username.jurisdiction_id) && 
                            uai.doc.name == jurisdiction_username.user_id
                        )
                        {
                            is_jurisdiction_ok = true;
                            break;
                        }
                    }

                    if(is_jurisdiction_ok)
                    {
                        break;
                    }


                    
                }



                if(is_jurisdiction_ok && is_app_prefix_ok) temp_list.Add (uai);
            }


            result.rows = temp_list;

            return result;

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return null;
        //return new mmria.common.model.couchdb.user[] { default(mmria.common.model.couchdb.user), default(mmria.common.model.couchdb.user) }; 
    } 

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.user> Get(string id) 
    { 
        mmria.common.model.couchdb.user result = null;
        try
        {
            string request_string = db_config.url + "/_users/" + id;

            var user_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            var responseFromServer = await user_curl.executeAsync();

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user>(responseFromServer);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);

        } 

        return result; 
    }


    [Authorize(Roles = "jurisdiction_admin,installation_admin")]
    [Route("check-user/{id}")]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.user> CheckUser(string id)
    {
        mmria.common.model.couchdb.user result = null;
        try
        {
            string request_string = db_config.url + "/_users/" + id;

            var user_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
            var responseFromServer = await user_curl.executeAsync();

                result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user>(responseFromServer);
            
            // If responseFromServer is null/empty, result remains as empty user object
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            // On exception, return empty user object instead of null
            //result = new mmria.common.model.couchdb.user();
        }

        return result;
    }

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.common.model.couchdb.document_put_response> Post([FromBody] mmria.common.model.couchdb.user user) 
    { 
        string object_string = null;
        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response ();

        try
        {

                
            if(string.IsNullOrWhiteSpace(db_config.prefix))
            {
                if(user.app_prefix_list == null)
                {
                    user.app_prefix_list = new Dictionary<string, bool>();
                }

                if(user.app_prefix_list.Count == 0 || !user.app_prefix_list.ContainsKey("__no_prefix__"))
                {
                    user.app_prefix_list.Add("__no_prefix__", true);
                }
            }
            else if(!user.app_prefix_list.ContainsKey(db_config.prefix))
            {
                user.app_prefix_list.Add(db_config.prefix, true);
            }

            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            object_string = Newtonsoft.Json.JsonConvert.SerializeObject(user, settings);

            

            string user_db_url = db_config.url + "/_users/"  + user._id;

            var user_curl = new cURL("PUT", null, user_db_url, object_string, db_config.user_name, db_config.user_value);
            var responseFromServer = await user_curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    } 

    [Authorize(Roles  = "jurisdiction_admin,installation_admin")]
    [HttpDelete]
    public async System.Threading.Tasks.Task<System.Dynamic.ExpandoObject> Delete(string user_id = null, string rev = null) 
    { 
        try
        {
            string request_string = null;

            bool is_only_remove_prefix = true;

            if (!string.IsNullOrWhiteSpace (user_id) && !string.IsNullOrWhiteSpace (rev)) 
            {
                request_string = db_config.url + "/_users/" + user_id + "?rev=" + rev;
            }
            else 
            {
                return null;
            }

            var delete_report_curl = new cURL ("DELETE", null, request_string, null, db_config.user_name, db_config.user_value);
            var check_document_curl = new cURL ("GET", null, db_config.url + "/_users/" + user_id, null, db_config.user_name, db_config.user_value);
                // check if doc exists
            mmria.common.model.couchdb.user user = null;

            try 
            {
                string document_json = null;
                document_json = await check_document_curl.executeAsync ();
                user = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.user> (document_json);
                
                
                
                if(string.IsNullOrWhiteSpace(db_config.prefix))
                {
                    if
                    (
                        user.app_prefix_list.Count == 0 ||
                        (
                            user.app_prefix_list.Count == 1 && 
                            user.app_prefix_list.ContainsKey("__no_prefix__")
                        )
                    )
                    {
                        is_only_remove_prefix = false;
                    }
                }
                else if(user.app_prefix_list.Count == 1 && user.app_prefix_list.ContainsKey(db_config.prefix))
                {
                    is_only_remove_prefix = false;
                }

                #if !IS_PMSS_ENHANCED
                if(!mmria.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, user))
                {
                    return null;
                }
                #endif
                #if IS_PMSS_ENHANCED
                if(!mmria.pmss.server.utils.authorization_user.is_authorized_to_handle_jurisdiction_id(db_config, User, user))
                {
                    return null;
                }
                #endif

            } 
            catch (Exception ex) 
            {
                // do nothing for now document doesn't exsist.
                System.Console.WriteLine ($"err caseController.Delete\n{ex}");
            }

            if(is_only_remove_prefix == false)
            {
                string responseFromServer = await delete_report_curl.executeAsync ();;
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject> (responseFromServer);

                return result;
            }
            else if(user != null)
            {

                user.app_prefix_list.Remove(db_config.prefix);
                
                string user_db_url = db_config.url + "/_users/"  + user._id;

                Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
                settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string object_string = Newtonsoft.Json.JsonConvert.SerializeObject(user, settings);

                var user_curl = new cURL("PUT", null, user_db_url, object_string, db_config.user_name, db_config.user_value);
                var responseFromServer = await user_curl.executeAsync();
                var put_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(responseFromServer);

                var result = new System.Dynamic.ExpandoObject();
                result.Append(new KeyValuePair<string, object>("ok", put_response.ok));
                result.Append(new KeyValuePair<string, object>("id", put_response.id));
                result.Append(new KeyValuePair<string, object>("rev", put_response.rev));

                return result;
            }


        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return null;
    }

} 


