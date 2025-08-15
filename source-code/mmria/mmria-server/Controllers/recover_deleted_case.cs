using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server.Controllers;

[Authorize(Roles  = "installation_admin,cdc_admin")]
[Route("recover-deleted-case/{action=Index}")]
public sealed class recover_deleted_caseController : Controller
{

    struct tombstone_struct
    {

        public string _id;
        public string _rev;
    }
    struct Selector_Struc
    {
        //public System.Dynamic.ExpandoObject selector;
        public System.Collections.Generic.Dictionary<string,System.Collections.Generic.Dictionary<string,string>> selector;
        public string[] fields;

        public string use_index;

        public int limit;
    }

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

   
    mmria.common.couchdb.ConfigurationSet ConfigDB;

    readonly mmria.common.couchdb.ConfigurationSet _dbConfigSet;


    public recover_deleted_caseController
    (

        mmria.common.couchdb.ConfigurationSet p_config_db,
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        mmria.common.couchdb.ConfigurationSet DbConfigurationSet
    )
    {

        ConfigDB = p_config_db;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);

        _dbConfigSet = DbConfigurationSet;

        if(_dbConfigSet.detail_list.ContainsKey("vital_import"))
        {
            _dbConfigSet.detail_list.Remove("vital_import");
        }
    }

    public IActionResult Index()
    {
        return View(_dbConfigSet);
    }


    public async Task<IActionResult> FindRecord(mmria.server.model.recover_deleted.Request Model)
    {
        var model = new mmria.server.model.recover_deleted.RequestResponse();
        model.SearchText = Model.RecordId;
        try
        {
            string responseFromServer  = null;

            if(Model.Role.Equals("cdc_admin", StringComparison.OrdinalIgnoreCase))
            {
                var db_info = _dbConfigSet.detail_list[Model.StateDatabase];
                string request_string = $"{db_info.url}/{db_info.prefix}audit/_design/sortable/_view/by_deleted?skip=0&limit=25000&descending=true";
                var case_view_curl = new cURL("GET", null, request_string, null, db_info.user_name, db_info.user_value);
                responseFromServer = await case_view_curl.executeAsync();

            }
            else
            {
             
                string request_string = $"{db_config.url}/{db_config.prefix}audit/_design/sortable/_view/by_deleted?skip=0&limit=25000&descending=true";
                var case_view_curl = new cURL("GET", null, request_string, null, db_config.user_name, db_config.user_value);
                responseFromServer = await case_view_curl.executeAsync();   
            }


            var audit_view_response = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.get_sortable_view_reponse_header<mmria.common.model.couchdb.audit.Audit_Detail_View>>(responseFromServer);

            foreach(var item in audit_view_response.rows)
            {
                try
                {
                    if
                    (
                        string.IsNullOrWhiteSpace(Model.RecordId) ||

                        item.value.record_id != null &&
                        (
                            item.value.record_id.IndexOf(Model.RecordId, System.StringComparison.OrdinalIgnoreCase) > -1 ||
                            Model.RecordId.IndexOf(item.value.record_id, System.StringComparison.OrdinalIgnoreCase) > -1
                        )
                    )
                    {
                        item.value._id = item.id;
                        item.value.StateDatabase = host_prefix;
                        model.Detail.Add(item.value);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }


        return View(model);
    }

    public IActionResult ConfirmRecoverRequest(mmria.common.model.couchdb.audit.Audit_Detail_View Model)
    {
        var model = Model;

    
        return View(model);
    }

    public sealed class UpdateDeletedCaseResult
    {
        public UpdateDeletedCaseResult(){}
        public mmria.common.model.couchdb.audit.Audit_Detail_View detail { get; set; }
        public bool is_problem_deleting { get; set; }
        public string problem_description { get; set; }
    }

    public async Task<IActionResult> UpdateDeletedCase(mmria.common.model.couchdb.audit.Audit_Detail_View Model)
    {
        var result = new UpdateDeletedCaseResult()
        {
            detail = Model,
            is_problem_deleting = false
        };

        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;


        try
        {
            var userName = "";
            if (User.Identities.Any(u => u.IsAuthenticated))
            {
                userName = User.Identities.First(
                    u => u.IsAuthenticated && 
                    u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
            }


            var db_info = _dbConfigSet.detail_list[Model.StateDatabase];

            string audit_url = $"{db_info.url}/{db_info.prefix}audit/{Model._id}";
            var audit_curl = new cURL("GET", null, audit_url, null, db_info.user_name, db_info.user_value);
            var audit_response = await audit_curl.executeAsync();
            var audit_object = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.Change_Stack>(audit_response);


            string get_revs_url = $"{db_info.url}/{db_info.prefix}mmrds/{audit_object.case_id}?revs=true&open_revs=all";
            var get_revs_curl = new cURL("GET", null, get_revs_url, null, db_info.user_name, db_info.user_value);
            var get_revs_curl_response = await get_revs_curl.executeAsync();
            var start_index = get_revs_curl_response.IndexOf("_rev");
            var end_index = get_revs_curl_response.IndexOf(",", start_index);
            var pre_current_rev = get_revs_curl_response.Substring(start_index,end_index - start_index);
            var current_rev = pre_current_rev.Replace("\"", "").Replace("_rev:","");


            var tombstone = new tombstone_struct();
            tombstone._id = audit_object._id;
            tombstone._rev = current_rev;



            string get_case_url = $"{db_info.url}/{db_info.prefix}mmrds/{audit_object.case_id}?rev={audit_object.delete_rev}";
            var get_case_curl = new cURL("GET", null, get_case_url, null, db_info.user_name, db_info.user_value);
            var get_case_response = await get_case_curl.executeAsync();
            var get_case_object = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(get_case_response);
            
            IDictionary<string, object> result_dictionary = get_case_object as IDictionary<string, object>;

            if(result_dictionary.ContainsKey("_rev"))
            {
                result_dictionary.Remove("_rev");
            }

            result_dictionary["date_last_updated"] = DateTime.Now;
            result_dictionary["last_updated_by"] = userName;

            var put_case_object_string = Newtonsoft.Json.JsonConvert.SerializeObject(get_case_object, settings);
             
            //string put_case_url = $"{db_info.url}/{db_info.prefix}mmrds/{audit_object.case_id}?rev={current_rev}";
            string put_case_url = $"{db_info.url}/{db_info.prefix}mmrds/{audit_object.case_id}";
            var put_case_curl = new cURL("PUT", null, put_case_url, put_case_object_string, db_info.user_name, db_info.user_value);
            
            try
            {
                var put_case_response = await put_case_curl.executeAsync();
                var put_result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(put_case_response);
                if(put_result.ok)
                {
                    string delete_audit_url = $"{db_info.url}/{db_info.prefix}audit/{Model._id}?rev={audit_object._rev}";
                    var delete_audit_curl = new cURL("DELETE", null, delete_audit_url, null, db_config.user_name, db_config.user_value);
                    var  delete_response = await delete_audit_curl.executeAsync();
                }
            }
            catch(Exception ex)
            {
                Console.Write("problem restoring deleted case\n{0}", ex);
                result.problem_description = ex.Message;

            }

            
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            result.is_problem_deleting = true;
            result.problem_description = ex.Message;
        }

        return View(result);
    }

}
