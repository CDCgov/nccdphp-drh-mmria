using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using  mmria.server.extension;

namespace mmria.server.Controllers;
    
[Authorize(Roles = "installation_admin,jurisdiction_admin")]
[Route("/manage-users/{action=Index}")]
public sealed class manage_usersController : Controller
{
 mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    IHttpContextAccessor httpContextAccessor;

    user_role_jurisdiction_viewController user_role_jurisdiction_view;

    public manage_usersController
    ( 
        IHttpContextAccessor p_httpContextAccessor,
        mmria.common.couchdb.OverridableConfiguration p_configuration
    )
    {

        httpContextAccessor = p_httpContextAccessor;

         configuration = p_configuration;

        host_prefix = p_httpContextAccessor.HttpContext.Request.Host.GetPrefix();

        db_config = configuration.GetDBConfig(host_prefix);
    }

    
    [HttpGet]
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
        var auditController = new _auditController(httpContextAccessor, configuration);
        /*
            /api/policyvalues
            /api/user_role_jurisdiction_view/my-roles
            /api/jurisdiction_tree
            /api/user_role_jurisdiction
            /api/user       
        */

        // policyvalues


        result["policy_values"] = policyValues.Get();
        result["my_roles"] = await user_role_jurisdiction_view.Get(0, -1, "by_user_id");
        result["jurisdiction_tree"] = await jurisdiction_treeController.Get();
        result["user_role_jurisdiction"] = await user_role_jurisdiction_view.Get(0, -1, "by_user_id");
        //result["user_role_jurisdiction"] = await user_role_jurisdictionController.Get(null);
        result["user_list"] = await userController.Get();
        result["manage_user_audit"] = await auditController.GetAuditDocument();

        return Json(result);
    }




    [Authorize(Roles = "installation_admin,jurisdiction_admin")]
    [Route("/form-manager")]
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

    public sealed class UserExportParams
    {
        public List<UserExportData> users { get; set; }
        public string title { get; set; } = "User Management Export";
    }

    public sealed class UserExportData
    {
        public string user_id { get; set; }
        public string role_name { get; set; }
        public string jurisdiction_id { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> ExportUsers
    (
        [FromBody]
        UserExportParams exportParams
    )
    {
        FastExcel.Row ConvertToUserRow(int p_row_number, UserExportData user)
        {
            var cells = new List<FastExcel.Cell>();

            cells.Add(new FastExcel.Cell(1, user.user_id ?? ""));
            cells.Add(new FastExcel.Cell(2, user.role_name ?? ""));
            cells.Add(new FastExcel.Cell(3, user.jurisdiction_id ?? ""));


            return new FastExcel.Row(p_row_number, cells);
        }   

        var Template_xlsx = "database-scripts/Template.xlsx";
        var Output_xlsx = System.IO.Path.Combine(configuration.GetString("export_directory", host_prefix), "UserExport.xlsx");

        if(Output_xlsx.StartsWith("/home/net_core_user/app/workdir/mmria-export"))
        {
            Template_xlsx = "/opt/app-root/src/source-code/mmria/mmria-server/database-scripts/Template.xlsx";
        }

        if(System.IO.File.Exists(Output_xlsx))
            System.IO.File.Delete(Output_xlsx);

        using (FastExcel.FastExcel fastExcel = new FastExcel.FastExcel(new System.IO.FileInfo(Template_xlsx), new System.IO.FileInfo(Output_xlsx)))
        {
            var worksheet = new FastExcel.Worksheet();
            var rows = new System.Collections.Generic.List<FastExcel.Row>();

            var row_number = 1;

            // Add column headers
            var columnHeaders = new List<FastExcel.Cell>();
            columnHeaders.Add(new FastExcel.Cell(1, "Username (Email Address)"));
            columnHeaders.Add(new FastExcel.Cell(2, "Role(s)"));
            columnHeaders.Add(new FastExcel.Cell(3, "Case Folder"));
            rows.Add(new FastExcel.Row(row_number, columnHeaders));

            // Add user data rows
            foreach (var user in exportParams.users ?? new List<UserExportData>())
            {
                row_number++;
                rows.Add(ConvertToUserRow(row_number, user));
            }

            worksheet.Rows = rows;
            fastExcel.Write(worksheet, "sheet1");
        }

        byte[] fileBytes = GetFile(Output_xlsx);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "UserManagementExport.xlsx");
    }

    byte[] GetFile(string s)
    {
        byte[] data;
        int br;
        int fs_length;

        using(System.IO.FileStream fs = new System.IO.FileStream(s, System.IO.FileMode.Open, System.IO.FileAccess.Read))
        {
            fs_length = (int) fs.Length;
            data = new byte[fs.Length];
            br = fs.Read(data, 0, data.Length);
        }
        if (br != (int) fs_length)
            throw new System.IO.IOException(s);
        return data;
    }
}
