using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;


using  mmria.server.extension;
using Akka.Streams.Implementation.Fusing;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using mmria.common.SharedLibraries.VitalImport.Manager;
namespace mmria.server;

public sealed class VitalImportPanelItem
{
    public string status_detail {get; set; }

    public string mmria_record_id {get; set; }
    public string cdc_unique_id{get; set; }
    public string last_name{get; set; }
    public string first_name{get; set; }
    public string date_of_birth{get; set; }
    public string date_of_death{get; set; }
    public string reporting_state{get; set; }
    public string state_of_death_record{get; set; }
}


[Authorize]
[Route("api/[controller]")]
public sealed class ije_messageController: ControllerBase 
{ 
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;
    private readonly VitalImportManager _vitalImportManager;

    public ije_messageController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        VitalImportManager vitalImportManager
    )
    {
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
        _vitalImportManager = vitalImportManager;
    }
    
    [Authorize(Roles  = "abstractor,jurisdiction_admin,data_analyst,vital_importer,vital_importer_state")]
    [HttpGet]
    public async Task<IActionResult> Get(string case_id) 
    { 

        mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch> result = null;

        try
        {
            mmria.common.couchdb.DBConfigurationDetail config = configuration.GetDBConfig("vital_import");

            //mmria.common.couchdb.DBConfigurationDetail config =  config_id_configuration.detail_list["vital_import"];
            result = await _vitalImportManager.GetBatchSetAsync(config);

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);

            var document_error = new mmria.common.model.couchdb.document_put_error();

            document_error.error = ex.Message;
            document_error.reason = ex.StackTrace;

            return Ok(document_error);

        }


        return Ok(result);
    }

    [Authorize(Roles  = "vital_importer")]
    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<bool> Delete() 
    { 
        bool result = false;
        try
        {

            string user_db_url = configuration.GetString("vitals_url",host_prefix).Replace("Message/IJESet", "VitalNotification");

            await _vitalImportManager.DeleteVitalNotificationAsync(
                user_db_url,
                configuration.GetString("vital_service_key",host_prefix));

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }

    [Authorize(Roles  = "vital_importer,vital_importer_state")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<mmria.server.model.NewIJESet_MessageResponse> Post()
    {
        var ijeset = await mmria.server.util.JsonRequestBodyReader.ReadAsync<mmria.server.model.NewIJESet_Message>(Request);
        string object_string = null;
        mmria.server.model.NewIJESet_MessageResponse result = new ();
        var sanitizedIjeSet = CreateSanitizedIjeSetMessage(ijeset);

        if (sanitizedIjeSet == null)
        {
            result.detail = "Invalid IJE payload.";
            return result;
        }

        try
        {
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            object_string = Newtonsoft.Json.JsonConvert.SerializeObject(sanitizedIjeSet, settings);

            string user_db_url = configuration.GetString("vitals_url",host_prefix);

            var responseFromServer = await _vitalImportManager.SubmitIjeSetAsync(
                user_db_url,
                object_string,
                configuration.GetString("vital_service_key",host_prefix));
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.server.model.NewIJESet_MessageResponse>(responseFromServer);

            if (!result.ok) 
            {

            }

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
            result.detail = ex.Message;
            
        }

        return result;
    } 

    [Authorize(Roles  = "vital_importer,vital_importer_state")]
    [Route("DownloadVitalImportExcel")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<FileContentResult> DownloadVitalImportExcel()
    {
        var vital_panel_list_json = await mmria.server.util.JsonRequestBodyReader.ReadAsync<dynamic[]>(Request);
        List<VitalImportPanelItem> vitalImportPanelItems = CreateSanitizedVitalImportPanelItems(vital_panel_list_json);
        await Task.CompletedTask;
        FastExcel.Row ConvertToDetail(int p_row_number, VitalImportPanelItem item)
        {
            var cells = new List<FastExcel.Cell>();

            cells.Add(new FastExcel.Cell(1, item.status_detail));
            cells.Add(new FastExcel.Cell(2, item.mmria_record_id));
            cells.Add(new FastExcel.Cell(3, item.cdc_unique_id));
            cells.Add(new FastExcel.Cell(4, item.last_name));
            cells.Add(new FastExcel.Cell(5, item.first_name));
            cells.Add(new FastExcel.Cell(6, item.date_of_birth));
            cells.Add(new FastExcel.Cell(7, item.date_of_death));
            cells.Add(new FastExcel.Cell(8, item.reporting_state));
            cells.Add(new FastExcel.Cell(9, item.state_of_death_record));
            return new FastExcel.Row(p_row_number, cells);
        }   

        var Template_xlsx = "database-scripts/Template.xlsx";
        var Output_xlsx = System.IO.Path.Combine (configuration.GetString("export_directory", host_prefix), "Output.xlsx");

        if(Output_xlsx.StartsWith("/home/net_core_user/app/workdir/mmria-export"))
        {
            Template_xlsx = "/opt/app-root/src/source-code/mmria/mmria-server/database-scripts/Template.xlsx";
        }
/*

        var Template_xlsx = "Template.xlsx";
        var Output_xlsx = "Output.xlsx";
*/
        if(System.IO.File.Exists(Output_xlsx))
            System.IO.File.Delete(Output_xlsx);

        using (FastExcel.FastExcel fastExcel = new FastExcel.FastExcel(new System.IO.FileInfo(Template_xlsx), new System.IO.FileInfo(Output_xlsx)))
        {
            //Create a worksheet with some rows
            var worksheet = new FastExcel.Worksheet();
            var rows = new List<FastExcel.Row>();

            var row_number = 1;
            var total = new mmria.server.utils.JurisdictionSummaryItem();

/*
            var header1 = new List<FastExcel.Cell>();
            header1.Add(new FastExcel.Cell(1, "MMRIA Jurisdiction Summary Report"));
            header1.Add(new FastExcel.Cell(2, ""));
            header1.Add(new FastExcel.Cell(3, ""));
            header1.Add(new FastExcel.Cell(4, ""));
            header1.Add(new FastExcel.Cell(5, ""));
            header1.Add(new FastExcel.Cell(6, ""));
            header1.Add(new FastExcel.Cell(7, ""));
            header1.Add(new FastExcel.Cell(8, ""));
            header1.Add(new FastExcel.Cell(9, ""));
            rows.Add(new FastExcel.Row(row_number, header1));
            row_number+=1;
*/

            var header = new List<FastExcel.Cell>();
            header.Add(new FastExcel.Cell(1, "Status Detail"));
            header.Add(new FastExcel.Cell(2, "MMRIA Record ID"));
            header.Add(new FastExcel.Cell(3, "CDC Unique ID"));
            header.Add(new FastExcel.Cell(4, "Last Name"));
            header.Add(new FastExcel.Cell(5, "First Name"));
            header.Add(new FastExcel.Cell(6, "Date of Birth"));
            header.Add(new FastExcel.Cell(7, "Date of Death"));
            header.Add(new FastExcel.Cell(8, "Reporting State"));
            header.Add(new FastExcel.Cell(9, "State of Death Record"));
            rows.Add(new FastExcel.Row(row_number, header));

    

            foreach (var item in vitalImportPanelItems)
            {
                row_number+=1;
                rows.Add(ConvertToDetail(row_number, item));
            }
            worksheet.Rows = rows;
            fastExcel.Write(worksheet, "sheet1");
        }

        byte[] fileBytes = GetFile(Output_xlsx);
        string exportDate = DateTime.Now.ToString("yyyy/MM/dd");
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"xlVitalsImportHistory_{exportDate.Split("/")[0]}-{exportDate.Split("/")[1]}-{exportDate.Split("/")[2]}.xlsx");
    }

    private static mmria.server.model.NewIJESet_Message CreateSanitizedIjeSetMessage(mmria.server.model.NewIJESet_Message request)
    {
        if (request == null)
        {
            return null;
        }

        return new mmria.server.model.NewIJESet_Message
        {
            mor = request.mor,
            nat = request.nat,
            fet = request.fet,
            mor_file_name = NormalizeOptionalString(request.mor_file_name),
            nat_file_name = NormalizeOptionalString(request.nat_file_name),
            fet_file_name = NormalizeOptionalString(request.fet_file_name),
            case_folder = NormalizeOptionalString(request.case_folder)
        };
    }

    private static List<VitalImportPanelItem> CreateSanitizedVitalImportPanelItems(dynamic[] values)
    {
        var result = new List<VitalImportPanelItem>();
        if (values == null)
        {
            return result;
        }

        foreach (var jsonItem in values)
        {
            if (jsonItem == null)
            {
                continue;
            }

            JObject vitalPanelItem;
            if (jsonItem is JObject existingObject)
            {
                vitalPanelItem = existingObject;
            }
            else
            {
                var rawValue = jsonItem.ToString();
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    continue;
                }

                dynamic deserializedJson = JsonConvert.DeserializeObject<dynamic>(rawValue);
                vitalPanelItem = JObject.Parse(deserializedJson.ToString());
            }

            result.Add(new VitalImportPanelItem
            {
                status_detail = ReadJObjectString(vitalPanelItem, "statusDetail"),
                mmria_record_id = ReadJObjectString(vitalPanelItem, "mmria_record_id"),
                cdc_unique_id = ReadJObjectString(vitalPanelItem, "cdcUniqueID"),
                last_name = ReadJObjectString(vitalPanelItem, "lastName"),
                first_name = ReadJObjectString(vitalPanelItem, "firstName"),
                date_of_birth = FormatDisplayDate(ReadJObjectString(vitalPanelItem, "dateOfBirth")),
                date_of_death = FormatDisplayDate(ReadJObjectString(vitalPanelItem, "dateOfDeath")),
                reporting_state = ReadJObjectString(vitalPanelItem, "reportingState"),
                state_of_death_record = ReadJObjectString(vitalPanelItem, "stateOfDeathRecord")
            });
        }

        return result;
    }

    private static string ReadJObjectString(JObject source, string key)
    {
        return NormalizeOptionalString(source?[key]?.ToString());
    }

    private static string FormatDisplayDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !DateTime.TryParse(value, out var parsedDate))
        {
            return string.Empty;
        }

        return parsedDate.ToString("MM/dd/yyyy");
    }

    private static string NormalizeOptionalString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    byte[] GetFile(string s)
    {
        byte[] data;
        int br;
        int fs_length;

        using(System.IO.FileStream fs = new System.IO.FileStream (s, System.IO.FileMode.Open, System.IO.FileAccess.Read))
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


