using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 

namespace mmria.server.Controllers;

[Authorize(Roles = "installation_admin,cdc_admin")]
public sealed class jurisdictionSummaryController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    mmria.common.couchdb.ConfigurationSet ConfigDB;

    public jurisdictionSummaryController
    (
        mmria.common.couchdb.ConfigurationSet p_config_db,
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
    {

        ConfigDB = p_config_db;
        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

    public async Task<IActionResult> Index(System.Threading.CancellationToken cancellationToken)
    {

        var result = new mmria.server.utils.JurisdictionSummary(ConfigDB);

        return View(await result.execute(cancellationToken));
    }


    public async Task<IActionResult> GenerateReport(System.Threading.CancellationToken cancellationToken)
    {

        var summary_list = new mmria.server.utils.JurisdictionSummary(ConfigDB);

        var summary_row_list = await summary_list.execute(cancellationToken);

        FastExcel.Row ConvertToDetail(int p_row_number, mmria.server.utils.JurisdictionSummaryItem item)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cells = new List<FastExcel.Cell>();

            cells.Add(new FastExcel.Cell(1, p_row_number));
            cells.Add(new FastExcel.Cell(2, item.host_name));
            cells.Add(new FastExcel.Cell(3, item.rpt_date));
            cells.Add(new FastExcel.Cell(4, item.num_recs));
            cells.Add(new FastExcel.Cell(5, item.num_users_unq));
            cells.Add(new FastExcel.Cell(6, item.num_users_ja));
            cells.Add(new FastExcel.Cell(7, item.num_users_abs));
            cells.Add(new FastExcel.Cell(8, item.num_user_anl));
            cells.Add(new FastExcel.Cell(9, item.num_user_cm));


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
            var rows = new System.Collections.Generic.List<FastExcel.Row>();

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
            header.Add(new FastExcel.Cell(1, "#"));
            header.Add(new FastExcel.Cell(2, "Jurisdiction Abbreviation"));
            header.Add(new FastExcel.Cell(3, "Report Date"));
            header.Add(new FastExcel.Cell(4, "# of Records"));
            header.Add(new FastExcel.Cell(5, "# of Unique MMRIA Users"));
            header.Add(new FastExcel.Cell(6, "Jurisdiction Admin"));
            header.Add(new FastExcel.Cell(7, "Abstractor"));
            header.Add(new FastExcel.Cell(8, "Analyst"));
            header.Add(new FastExcel.Cell(9, "Committee Member"));
            rows.Add(new FastExcel.Row(row_number, header));

    

            foreach (var item in summary_row_list)
            {
                cancellationToken.ThrowIfCancellationRequested();

                row_number+=1;
                
                if(total.num_recs > -1)
                total.num_recs += item.num_recs;

                if(total.num_users_unq > -1)
                total.num_users_unq += item.num_users_unq;
                
                if(total.num_users_ja > -1)
                total.num_users_ja += item.num_users_ja;
                
                if(total.num_users_abs > -1)
                total.num_users_abs += item.num_users_abs;
                
                if(total.num_user_anl > -1)
                total.num_user_anl += item.num_user_anl;
                
                if(total.num_user_cm > -1)
                total.num_user_cm += item.num_user_cm;

                rows.Add(ConvertToDetail(row_number, item));

            }

            row_number+=1;
            var footer = new List<FastExcel.Cell>();
            footer.Add(new FastExcel.Cell(1, ""));
            footer.Add(new FastExcel.Cell(2, "Total"));
            footer.Add(new FastExcel.Cell(3, ""));
            footer.Add(new FastExcel.Cell(4, total.num_recs));
            footer.Add(new FastExcel.Cell(5, total.num_users_unq));
            footer.Add(new FastExcel.Cell(6, total.num_users_ja));
            footer.Add(new FastExcel.Cell(7, total.num_users_abs));
            footer.Add(new FastExcel.Cell(8, total.num_user_anl));
            footer.Add(new FastExcel.Cell(9, total.num_user_cm));
            rows.Add(new FastExcel.Row(row_number, footer));

            worksheet.Rows = rows;

            fastExcel.Write(worksheet, "sheet1");
        }

        byte[] fileBytes = GetFile(Output_xlsx);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlJurisdictionCounts.xlsx");;
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
