using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using  mmria.server.extension;
using mmria.server.util;

namespace mmria.server.Controllers;
    

public sealed class _usersController : Controller
{
    IHttpContextAccessor httpContextAccessor;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;
    private readonly mmria.server.util.RequestTenantRuntime _tenantRuntime;

    public _usersController
    ( 
        IHttpContextAccessor p_httpContextAccessor,
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient
    )
    {
        httpContextAccessor = p_httpContextAccessor;
        _couchDbHttpClient = couchDbHttpClient;
        _tenantRuntime = tenantRuntime;
        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [Authorize(Roles = "installation_admin,jurisdiction_admin")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetInitialData()
    {
        var result = new Dictionary<string,object>();
        var manageUsersManager = new mmria.common.SharedLibraries.ManageUsers.Manager.ManageUsersManager(
            new mmria.common.SharedLibraries.ManageUsers.DAL.ManageUsersDAL(_couchDbHttpClient),
            _couchDbHttpClient
        );

        var policyValues = new policyValuesController(httpContextAccessor, _tenantRuntime);
        var user_role_jurisdiction_view = new user_role_jurisdiction_viewController(httpContextAccessor, _tenantRuntime, manageUsersManager);
        var jurisdiction_treeController = new jurisdiction_treeController(httpContextAccessor, _tenantRuntime, _couchDbHttpClient);
        var user_role_jurisdictionController = new user_role_jurisdictionController(httpContextAccessor, _tenantRuntime, manageUsersManager, _couchDbHttpClient);
        var userController = new userController(httpContextAccessor, _tenantRuntime, manageUsersManager);
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

        return EscapedJsonResultFactory.Create(result);
    }

    [Authorize(Roles = "installation_admin,jurisdiction_admin")]
    public IActionResult FormManager()
    {
        return View();
    }



    [Authorize(Roles = "installation_admin,jurisdiction_admin, abstractor, data_analyst, committee_member, vro")]
    public async Task<IActionResult> GetFormAccess()
    {
        var result = await LoadFormAccessSpecificationAsync();

        return EscapedJsonResultFactory.Create(result);

    }

    [HttpPost]
    public async Task<IActionResult> SetFormAccess()
    {
        var request = await JsonRequestBodyReader.ReadAsync<FormAccessSaveRequest>(Request);

        mmria.common.model.couchdb.document_put_response result = null;

        if (request == null)
        {
            result = new mmria.common.model.couchdb.document_put_response()
            {
                error_description = "Invalid form-access request."
            };
            return EscapedJsonResultFactory.Create(result);
        }

        if(request._id != "form-access-list")
        {
            result = new mmria.common.model.couchdb.document_put_response()
            {
                error_description = "Invalid form-access request."
            };
            return EscapedJsonResultFactory.Create(result);
        }

        var userName = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }

        var existingRequest = await LoadFormAccessSpecificationAsync();
        var sanitizedRequest = CreateSanitizedFormAccessSpecification(request, existingRequest, userName);


        Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
        settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        var object_string = Newtonsoft.Json.JsonConvert.SerializeObject(sanitizedRequest, settings);

        string metadata_url = db_config.Get_Prefix_DB_Url($"jurisdiction/form-access-list");
        string save_response_from_server = null;
        try
        {
            save_response_from_server = await _couchDbHttpClient.ExecuteAsync(
                "PUT",
                metadata_url,
                object_string,
                db_config.user_name,
                db_config.user_value
            );
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.document_put_response>(save_response_from_server);
        }
        catch(Exception ex)
        {
            result ??= new mmria.common.model.couchdb.document_put_response();
            result.error_description = "Unable to save form access.";
            Console.WriteLine(ex);
        }

        return EscapedJsonResultFactory.Create(result);

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

    public sealed class FormAccessSaveRequest
    {
        public string _id { get; set; }
        public string _rev { get; set; }
        public List<FormAccess> access_list { get; set; }
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

    private async Task<FormAccessSpecification> LoadFormAccessSpecificationAsync()
    {
        var result = new FormAccessSpecification();

        string metadata_url = db_config.Get_Prefix_DB_Url($"jurisdiction/form-access-list");
        string save_response_from_server = null;
        try
        {
            save_response_from_server = await _couchDbHttpClient.ExecuteAsync(
                "GET",
                metadata_url,
                null,
                db_config.user_name,
                db_config.user_value
            );
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<FormAccessSpecification>(save_response_from_server) ?? new FormAccessSpecification();
        }
        catch(System.Net.WebException ex)
        {
            if(ex.Message.IndexOf("404") > -1)
            {
                SeedDefaultFormAccessSpecification(result);
            }
            else
            {
              Console.WriteLine(ex);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }

        return result;
    }

    private static void SeedDefaultFormAccessSpecification(FormAccessSpecification result)
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

    private static FormAccessSpecification CreateSanitizedFormAccessSpecification(
        FormAccessSaveRequest request,
        FormAccessSpecification existing,
        string userName)
    {
        var sanitizedRequest = new FormAccessSpecification
        {
            _id = "form-access-list",
            _rev = string.IsNullOrWhiteSpace(existing?._rev) ? request?._rev : existing._rev,
            date_created = existing != null && existing.date_created != default ? existing.date_created : DateTime.UtcNow,
            created_by = !string.IsNullOrWhiteSpace(existing?.created_by) ? existing.created_by : userName,
            date_last_updated = DateTime.UtcNow,
            last_updated_by = userName,
            access_list = request?.access_list?
                .Where(i => i != null)
                .Select(i => new FormAccess
                {
                    form_path = i.form_path,
                    abstractor = i.abstractor,
                    data_analyst = i.data_analyst,
                    committee_member = i.committee_member,
                    vro = i.vro
                }).ToList() ?? new List<FormAccess>()
        };

        return sanitizedRequest;
    }

}
