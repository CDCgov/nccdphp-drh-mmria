using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using  mmria.server.extension;

namespace mmria.server.Controllers;

[AllowAnonymous] 


public sealed class view_data_summaryController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    mmria.common.couchdb.ConfigurationSet ConfigDB;

    public view_data_summaryController    
    (
        IHttpContextAccessor httpContextAccessor, 
        mmria.server.util.RequestTenantRuntime tenantRuntime
    )
    {
        ConfigDB = tenantRuntime.RequireConfigurationSet();
        host_prefix = tenantRuntime.EffectiveHostPrefix;
        configuration = tenantRuntime.RequireConfiguration();

        db_config = tenantRuntime.RequireDbConfig();
    }

    [Route("view-data-summary")]
    public IActionResult Index()
    {
        return View();
    }

    public class ReportParams
    {
        public string fn { get; set;}
        public string fs { get; set;}
        public string fd { get; set;}
    }

    [Route("view-data-summary/GenerateReport")]
    [HttpPost]
    public async Task<IActionResult> GenerateReport
    (
        [FromBody]
        ReportParams rp
    )
    {
        var safeParams = CreateSanitizedReportParams(rp);
        if (safeParams == null)
        {
            return BadRequest();
        }

        var summary_row_list = safeParams.fd.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        FastExcel.Row ConvertToDetail(int p_row_number, string item)
        {

            var cells = new List<FastExcel.Cell>();

            cells.Add(new FastExcel.Cell(1, p_row_number -1));
            cells.Add(new FastExcel.Cell(2, item));

            return new FastExcel.Row(p_row_number, cells);

        }   

        var Template_xlsx = "database-scripts/Template.xlsx";
        var Output_xlsx = System.IO.Path.Combine (configuration.GetString("export_directory", host_prefix), "Output.xlsx");

        if(Output_xlsx.StartsWith("/home/net_core_user/app/workdir/mmria-export"))
        {
            Template_xlsx = "/opt/app-root/src/source-code/mmria/mmria-server/database-scripts/Template.xlsx";
        }
        

/*

    fn = 'Home Record - Year home_record/date_of_death/year'
    fs = 'N (Total Count)  : 721'

        var Template_xlsx = "Template.xlsx";
        var Output_xlsx = "Output.xlsx";
*/
        if(System.IO.File.Exists(Output_xlsx))
            System.IO.File.Delete(Output_xlsx);

        using (FastExcel.FastExcel fastExcel = new FastExcel.FastExcel(new System.IO.FileInfo(Template_xlsx), new System.IO.FileInfo(Output_xlsx)))
        {
            //Create a worksheet with some rows
            var worksheet = new FastExcel.Worksheet();
            var rows = new System.Collections.Generic.List<FastExcel.Row>();

            var row_number = 1;

            var header = new List<FastExcel.Cell>();
            header.Add(new FastExcel.Cell(1, $"View Data Summary {safeParams.fn} {safeParams.fs}"));
            rows.Add(new FastExcel.Row(row_number, header));

            foreach (var item in summary_row_list)
            {
                row_number+=1;
                rows.Add(ConvertToDetail(row_number, item));

            }

            worksheet.Rows = rows;

            fastExcel.Write(worksheet, "sheet1");
        }

        byte[] fileBytes = GetFile(Output_xlsx);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlJurisdictionCounts.xlsx");;
    }

    private static ReportParams CreateSanitizedReportParams(ReportParams request)
    {
        if (request == null)
        {
            return null;
        }

        return new ReportParams
        {
            fn = string.IsNullOrWhiteSpace(request.fn) ? string.Empty : request.fn.Trim(),
            fs = string.IsNullOrWhiteSpace(request.fs) ? string.Empty : request.fs.Trim(),
            fd = request.fd ?? string.Empty
        };
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
