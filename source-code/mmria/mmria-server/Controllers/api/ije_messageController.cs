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

    mmria.common.couchdb.ConfigurationSet config_id_configuration;
    string host_prefix = null;

    public ije_messageController
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration,
        mmria.common.couchdb.ConfigurationSet _config_id_configuration
    )
    {
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);

        config_id_configuration =  _config_id_configuration;
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
            
            string url = $"{config.url}/vital_import/_all_docs?include_docs=true";


            var user_curl = new cURL("GET", null, url, null, config.user_name, config.user_value);

            var responseFromServer = await user_curl.executeAsync();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.model.couchdb.alldocs_response<mmria.common.ije.Batch>>(responseFromServer);

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
    public async Task<bool> Delete() 
    { 
        bool result = false;
        try
        {

            string user_db_url = configuration.GetString("vitals_url",host_prefix).Replace("Message/IJESet", "VitalNotification");

            var user_curl = new cURL("DELETE", null, user_db_url, null);
            user_curl.AddHeader("vital-service-key", configuration.GetString("vital_service_key",host_prefix));
            var responseFromServer = await user_curl.executeAsync();

        }
        catch(Exception ex) 
        {
            Console.WriteLine (ex);
        }

        return result;
    }

    [Authorize(Roles  = "vital_importer,vital_importer_state")]
    [HttpPost]
    public async System.Threading.Tasks.Task<mmria.server.model.NewIJESet_MessageResponse> Post([FromBody] mmria.server.model.NewIJESet_Message ijeset) 
    { 
        string object_string = null;
        mmria.server.model.NewIJESet_MessageResponse result = new ();

        try
        {
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings ();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            object_string = Newtonsoft.Json.JsonConvert.SerializeObject(ijeset, settings);

                //var localUrl = "https://localhost:44331/api/Message/IJESet";
                //var message_curl = new mmria.server.cURL("POST", null, localUrl, message);
                //var messge_curl_result = await message_curl.executeAsync();

            string user_db_url = configuration.GetString("vitals_url",host_prefix);

            var user_curl = new cURL("PUT", null, user_db_url, object_string);
            user_curl.AddHeader("vital-service-key", configuration.GetString("vital_service_key",host_prefix));
            var responseFromServer = await user_curl.executeAsync();
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
    public async Task<FileContentResult> DownloadVitalImportExcel([FromBody] dynamic[] vital_panel_list_json)
    {
        List<VitalImportPanelItem> vitalImportPanelItems = new List<VitalImportPanelItem>();
        foreach(dynamic jsonItem in vital_panel_list_json)
        {
            dynamic deserializedJson = JsonConvert.DeserializeObject<dynamic>(jsonItem.ToString());
            JObject vitalPanelItem = JObject.Parse(deserializedJson.ToString());
            vitalImportPanelItems.Add(
            new VitalImportPanelItem
            {
                status_detail = vitalPanelItem["statusDetail"].ToString(),
                mmria_record_id = vitalPanelItem["mmria_record_id"].ToString(),
                cdc_unique_id = vitalPanelItem["cdcUniqueID"].ToString(),
                last_name = vitalPanelItem["lastName"].ToString(),
                first_name = vitalPanelItem["firstName"].ToString(),
                date_of_birth = vitalPanelItem["dateOfBirth"].ToString().Split("-")[1] + "/" + vitalPanelItem["dateOfBirth"].ToString().Split("-")[2] + "/" + vitalPanelItem["dateOfBirth"].ToString().Split("-")[0],
                date_of_death = vitalPanelItem["dateOfDeath"].ToString().Split("-")[1] + "/" + vitalPanelItem["dateOfDeath"].ToString().Split("-")[2] + "/" + vitalPanelItem["dateOfDeath"].ToString().Split("-")[0],
                reporting_state = vitalPanelItem["reportingState"].ToString(),
                state_of_death_record = vitalPanelItem["stateOfDeathRecord"].ToString()
            }
            );
        }
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


