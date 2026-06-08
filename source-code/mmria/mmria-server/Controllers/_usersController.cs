using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using mmria.common.SharedLibraries.Jurisdiction.Manager;
using mmria.common.SharedLibraries.ManageUsers.Manager;
using  mmria.server.extension;
using mmria.server.util;
using SharedFormAccess = mmria.common.SharedLibraries.ManageUsers.Model.FormAccess;
using SharedFormAccessSpecification = mmria.common.SharedLibraries.ManageUsers.Model.FormAccessSpecification;

namespace mmria.server.Controllers;
    

public sealed class _usersController : Controller
{
    IHttpContextAccessor httpContextAccessor;
    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly mmria.server.util.RequestTenantRuntime _tenantRuntime;
    private readonly ManageUsersManager _manageUsersManager;
    private readonly JurisdictionManager _jurisdictionManager;

    public _usersController
    ( 
        IHttpContextAccessor p_httpContextAccessor,
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        ManageUsersManager manageUsersManager,
        JurisdictionManager jurisdictionManager
    )
    {
        httpContextAccessor = p_httpContextAccessor;
        _tenantRuntime = tenantRuntime;
        _manageUsersManager = manageUsersManager;
        _jurisdictionManager = jurisdictionManager;
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
        var policyValues = new policyValuesController(httpContextAccessor, _tenantRuntime);
        var userController = new userController(httpContextAccessor, _tenantRuntime, _manageUsersManager);

        result["policy_values"] = policyValues.Get();
        result["my_roles"] = await _manageUsersManager.GetUserRoleJurisdictionViewAsync(0, 25, "by_date_created", null, false, httpContextAccessor.HttpContext.User, db_config);
        result["jurisdiction_tree"] = await _jurisdictionManager.GetJurisdictionTreeAsync(db_config);
        result["user_role_jurisdiction"] = await _manageUsersManager.GetUserRoleJurisdictionsAsync(null, httpContextAccessor.HttpContext.User, db_config);
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


        try
        {
            result = await _manageUsersManager.SaveFormAccessAsync(ToSharedFormAccessSpecification(sanitizedRequest), userName, db_config);
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
        try
        {
            return ToControllerFormAccessSpecification(await _manageUsersManager.GetFormAccessAsync(db_config));
        }
        catch(System.Net.WebException ex)
        {
            if(ex.Message.IndexOf("404") > -1)
            {
                var result = new FormAccessSpecification();
                SeedDefaultFormAccessSpecification(result);
                return result;
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

        return new FormAccessSpecification();
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

    private static FormAccessSpecification ToControllerFormAccessSpecification(SharedFormAccessSpecification value)
    {
        if (value == null)
        {
            return new FormAccessSpecification();
        }

        return new FormAccessSpecification
        {
            _id = value._id,
            _rev = value._rev,
            date_created = value.date_created,
            created_by = value.created_by,
            date_last_updated = value.date_last_updated,
            last_updated_by = value.last_updated_by,
            access_list = value.access_list?.Select(i => new FormAccess
            {
                form_path = i.form_path,
                abstractor = i.abstractor,
                data_analyst = i.data_analyst,
                committee_member = i.committee_member,
                vro = i.vro
            }).ToList() ?? new List<FormAccess>()
        };
    }

    private static SharedFormAccessSpecification ToSharedFormAccessSpecification(FormAccessSpecification value)
    {
        if (value == null)
        {
            return null;
        }

        return new SharedFormAccessSpecification
        {
            _id = value._id,
            _rev = value._rev,
            date_created = value.date_created,
            created_by = value.created_by,
            date_last_updated = value.date_last_updated,
            last_updated_by = value.last_updated_by,
            access_list = value.access_list?.Select(i => new SharedFormAccess
            {
                form_path = i.form_path,
                abstractor = i.abstractor,
                data_analyst = i.data_analyst,
                committee_member = i.committee_member,
                vro = i.vro
            }).ToList() ?? new List<SharedFormAccess>()
        };
    }

}
