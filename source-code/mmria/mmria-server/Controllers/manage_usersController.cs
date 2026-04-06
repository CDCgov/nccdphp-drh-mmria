using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using mmria.common.utils;
using  mmria.server.extension;
using mmria.common.SharedLibraries.ManageUsers.Manager;
using SharedFormAccess = mmria.common.SharedLibraries.ManageUsers.Model.FormAccess;
using SharedFormAccessSpecification = mmria.common.SharedLibraries.ManageUsers.Model.FormAccessSpecification;
using mmria.server.util;

namespace mmria.server.Controllers;
    
[Authorize(Roles = "installation_admin,jurisdiction_admin")]
[Route("/manage-users/{action=Index}")]
public sealed class manage_usersController : Controller
{
 mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    IHttpContextAccessor httpContextAccessor;
    private readonly ManageUsersManager _manageUsersManager;

    public manage_usersController
    ( 
        IHttpContextAccessor p_httpContextAccessor,
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        ManageUsersManager manageUsersManager
    )
    {

        httpContextAccessor = p_httpContextAccessor;
        _manageUsersManager = manageUsersManager;


        host_prefix = tenantRuntime.EffectiveHostPrefix;

        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }


    [HttpGet]

    public async Task<IActionResult> GetInitialData()
    {
        var result = await _manageUsersManager.GetInitialDataAsync(User, configuration, host_prefix, db_config);
        return EscapedJsonResultFactory.Create(result);
    }




    [Authorize(Roles = "installation_admin,jurisdiction_admin")]
    [Route("/form-manager")]
    public IActionResult FormManager()
    {
        return View();
    }

    [Authorize(Roles = "installation_admin,jurisdiction_admin, abstractor, data_analyst, committee_member, vro")]
    public async Task<IActionResult> GetFormAccess()
    {
        var result = new FormAccessSpecification();
        try
        {
            var sharedResult = await _manageUsersManager.GetFormAccessAsync(db_config);
            result = ToControllerFormAccessSpecification(sharedResult);
        }
        catch(Exception ex)
        {
            //result.error_description = ex.ToString();
            Console.WriteLine(ex);
        }

        return EscapedJsonResultFactory.Create(result);

    }

    [HttpPost]
    public async Task<IActionResult> SetFormAccess()
    {
        var request = await JsonRequestBodyReader.ReadAsync<FormAccessSaveRequest>(Request);

        mmria.common.model.couchdb.document_put_response result = new mmria.common.model.couchdb.document_put_response();

        if (request == null)
        {
            result.error_description = "Invalid request.";
            return EscapedJsonResultFactory.Create(result);
        }

        if(request._id != "form-access-list")
        {
            result.error_description = "Invalid request.";
            return EscapedJsonResultFactory.Create(result);
        }

        var userName = "";
        if (User.Identities.Any(u => u.IsAuthenticated))
        {
            userName = User.Identities.First(
                u => u.IsAuthenticated && 
                u.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Name)).FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }

        try
        {
            var existingRequest = ToControllerFormAccessSpecification(await _manageUsersManager.GetFormAccessAsync(db_config));
            var revisionHandling = CouchDbRevisionHelper.DescribeRevisionHandling(request?._rev, existingRequest?._rev);
            var sanitizedRequest = CreateSanitizedFormAccessSpecification(request, existingRequest, userName);
            result = await _manageUsersManager.SaveFormAccessAsync(ToSharedFormAccessSpecification(sanitizedRequest), userName, db_config);
            if (result == null || !result.ok)
            {
                Console.WriteLine(
                    $"Form access save failed for form-access-list: rev={revisionHandling}; response={result?.error_description}");
            }
        }
        catch(Exception ex)
        {
            result.error_description = "Failed to save form access.";
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
    public async Task<IActionResult> ExportUsers()
    {
        var exportParams = await JsonRequestBodyReader.ReadAsync<UserExportParams>(Request);
        var sanitizedExportParams = CreateSanitizedUserExportParams(exportParams);

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
            foreach (var user in sanitizedExportParams.users ?? new List<UserExportData>())
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

    private static UserExportParams CreateSanitizedUserExportParams(UserExportParams value)
    {
        return new UserExportParams
        {
            title = string.IsNullOrWhiteSpace(SanitizeSingleLineText(value?.title, 200))
                ? "User Management Export"
                : SanitizeSingleLineText(value.title, 200),
            users = value?.users?
                .Where(user => user != null)
                .Select(CreateSanitizedUserExportData)
                .ToList() ?? new List<UserExportData>()
        };
    }

    private static UserExportData CreateSanitizedUserExportData(UserExportData value)
    {
        return new UserExportData
        {
            user_id = SanitizeSingleLineText(value?.user_id, 512),
            role_name = SanitizeSingleLineText(value?.role_name, 256),
            jurisdiction_id = SanitizeSingleLineText(value?.jurisdiction_id, 256)
        };
    }

    private static string SanitizeSingleLineText(string value, int maxLength = 512)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return sanitized.Length > maxLength
            ? sanitized[..maxLength]
            : sanitized;
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

    private static FormAccessSpecification CreateSanitizedFormAccessSpecification(
        FormAccessSaveRequest request,
        FormAccessSpecification existing,
        string userName)
    {
        var sanitizedRequest = new FormAccessSpecification
        {
            _id = "form-access-list",
            _rev = CouchDbRevisionHelper.ResolveServerOwnedRevision(request?._rev, existing?._rev),
            date_created = existing != null && existing.date_created != default ? existing.date_created : DateTime.UtcNow,
            created_by = !string.IsNullOrWhiteSpace(existing?.created_by) ? existing.created_by : userName,
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
