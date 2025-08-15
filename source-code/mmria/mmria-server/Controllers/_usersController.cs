using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using  mmria.server.extension;

namespace mmria.server.Controllers;
    

public sealed class _usersController : Controller
{
    IHttpContextAccessor httpContextAccessor;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    public _usersController
    ( 
        IHttpContextAccessor p_httpContextAccessor,
        mmria.common.couchdb.OverridableConfiguration p_configuration
    )
    {
        httpContextAccessor = p_httpContextAccessor;
        
        configuration = p_configuration;

        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        db_config = configuration.GetDBConfig(host_prefix);
    }

    [Authorize(Roles = "installation_admin,jurisdiction_admin")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<JsonResult> GetInitialData()
    {
        var result = new Dictionary<string,object>();

        var policyValues = new policyValuesController(httpContextAccessor, configuration);
        var user_role_jurisdiction_view = new user_role_jurisdiction_viewController(httpContextAccessor, configuration);
        var jurisdiction_treeController = new jurisdiction_treeController(httpContextAccessor, configuration);
        var user_role_jurisdictionController = new user_role_jurisdictionController(httpContextAccessor, configuration);
        var userController = new userController(httpContextAccessor, configuration);
        /*
            /api/policyvalues
            /api/user_role_jurisdiction_view/my-roles
            /api/jurisdiction_tree
            /api/user_role_jurisdiction
            /api/user       
        */

// policyvalues


        result["policy_values"] = policyValues.Get();
        result["my_roles"] = await user_role_jurisdiction_view.Get();
        result["jurisdiction_tree"] = await jurisdiction_treeController.Get();
        result["user_role_jurisdiction"] = await user_role_jurisdictionController.Get(null);
        result["user_list"] = await userController.Get();

        return Json(result);
    }

    [Authorize(Roles = "installation_admin,jurisdiction_admin")]
    public IActionResult FormManager()
    {
        return View();
    }



    [Authorize(Roles = "installation_admin,jurisdiction_admin, abstractor, data_analyst, committee_member, vro")]
    public async Task<JsonResult> GetFormAccess()
    {
        var result = new FormAccessSpecification();

        string metadata_url = db_config.Get_Prefix_DB_Url($"jurisdiction/form-access-list");
        cURL document_curl = new cURL ("GET", null, metadata_url, null, db_config.user_name, db_config.user_value);
        
        string save_response_from_server = null;
        try
        {
            save_response_from_server = await document_curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<FormAccessSpecification>(save_response_from_server);
        }
        catch(System.Net.WebException ex)
        {
            if(ex.Message.IndexOf("404") > -1)
            {
                result._id = "form-access-list";
                result.created_by = "system";
                result.date_created = DateTime.UtcNow;

                result.last_updated_by = "system";
                result.date_last_updated = DateTime.UtcNow;

                result.access_list.Add(new FormAccess() { form_path = "/tracking", abstractor="view, edit", data_analyst="view", committee_member="view", vro="no_access" });
                result.access_list.Add(new FormAccess() { form_path = "/demographic", abstractor="view, edit", data_analyst="view", committee_member="view", vro="no_access" });
                result.access_list.Add(new FormAccess() { form_path = "/outcome", abstractor="view, edit", data_analyst="view", committee_member="view", vro="no_access" });
                result.access_list.Add(new FormAccess() { form_path = "/cause_of_death", abstractor="view, edit", data_analyst="view", committee_member="view, edit", vro="no_access" });
                result.access_list.Add(new FormAccess() { form_path = "/preparer_remarks", abstractor="view, edit", data_analyst="view", committee_member="view", vro="no_access" });
                result.access_list.Add(new FormAccess() { form_path = "/committee_review", abstractor="view", data_analyst="view", committee_member="view, edit", vro="no_access" });
                result.access_list.Add(new FormAccess() { form_path = "/vro_case_determination", abstractor="view", data_analyst="view", committee_member="view", vro="view, edit" });
                result.access_list.Add(new FormAccess() { form_path = "/ije_dc", abstractor="view", data_analyst="view", committee_member="view", vro="no_access" });
                result.access_list.Add(new FormAccess() { form_path = "/ije_bc", abstractor="view", data_analyst="view", committee_member="view", vro="no_access" });
                result.access_list.Add(new FormAccess() { form_path = "/ije_fetaldc", abstractor="view", data_analyst="view", committee_member="view", vro="no_access" });
                result.access_list.Add(new FormAccess() { form_path = "/amss_tracking", abstractor="view, edit", data_analyst="view", committee_member="view, edit", vro="no_access" });

            }
            else
            {
              Console.WriteLine(ex);
            }
        }
        catch(Exception ex)
        {
            //result.error_description = ex.ToString();
            Console.WriteLine(ex);
        }

        return Json(result);

    }


    public async Task<JsonResult> SetFormAccess
    (
        [FromBody] FormAccessSpecification request
    )
    {

        mmria.common.model.couchdb.document_put_response result = null;

        if(request._id != "form-access-list")
        {
            result = new mmria.common.model.couchdb.document_put_response()
            {
                error_description = $"invalid request._id: found {request._id}"
            };
            return Json(result);
        }

        var userName = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }

        request.last_updated_by = userName;
        request.date_last_updated = DateTime.UtcNow;


        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(request, settings);

        string metadata_url = db_config.Get_Prefix_DB_Url($"jurisdiction/form-access-list");
        cURL document_curl = new cURL ("PUT", null, metadata_url, object_string,db_config.user_name, db_config.user_value);
        
        string save_response_from_server = null;
        try
        {
            save_response_from_server = await document_curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(save_response_from_server);
        }
        catch(Exception ex)
        {
            result.error_description = ex.ToString();
            Console.WriteLine(ex);
        }

        return Json(result);

    }

    public sealed class FormAccess
    {
        public FormAccess(){}

        public string form_path { get; set; }
        public string abstractor { get; set; }
        public string data_analyst { get; set; }
        public string committee_member { get; set; }
        public string vro { get; set; }
    }

    public sealed class FormAccessSpecification
    {

        public FormAccessSpecification()
        {
            access_list = new List<FormAccess>();
        }

        public string _id { get; set;}
        public string _rev { get; set; }
        public string data_type { get; } = "form-access-specification";

        public DateTime date_created { get; set; } 
        public string created_by { get; set; } 
        public DateTime date_last_updated { get; set; } 
        public string last_updated_by { get; set; } 

        public List<FormAccess> access_list { get; set;}
    }

}
