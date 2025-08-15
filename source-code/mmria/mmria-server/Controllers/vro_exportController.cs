#if IS_PMSS_ENHANCED
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
public sealed class vro_exportController : Controller
{
    mmria.common.couchdb.OverridableConfiguration configuration;
    mmria.common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    mmria.common.couchdb.ConfigurationSet ConfigDB;

    public vro_exportController
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

        var result = new mmria.server.utils.VROSummary(configuration, host_prefix);

        return View(await result.execute(cancellationToken));
    }


    public async Task<IActionResult> GenerateReport(System.Threading.CancellationToken cancellationToken)
    {

        var summary_list = new mmria.server.utils.VROSummary(configuration, host_prefix);

        var summary_row_list = await summary_list.execute(cancellationToken);

        FastExcel.Row ConvertToDetail(int p_row_number, mmria.server.utils.VROSummaryItem item)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cells = new List<FastExcel.Cell>();

            cells.Add(new FastExcel.Cell(1, p_row_number));
            cells.Add(new FastExcel.Cell( 2,item.Death_Year));
            cells.Add(new FastExcel.Cell( 3,item.Jurisdiction_Abrev));
            cells.Add(new FastExcel.Cell( 4,item.Jurisdiction_Name));
            cells.Add(new FastExcel.Cell( 5,item.DC_AuxNo));
            cells.Add(new FastExcel.Cell( 6,item.DC_FileNo));
            cells.Add(new FastExcel.Cell( 7,item.DC_DOD));
            cells.Add(new FastExcel.Cell( 8,item.DC_TimingOfDeath));
            cells.Add(new FastExcel.Cell( 9,item.DC_Cod33A));
            cells.Add(new FastExcel.Cell( 10,item.DC_Cod33B));
            cells.Add(new FastExcel.Cell( 11,item.DC_Cod33C));
            cells.Add(new FastExcel.Cell( 12,item.DC_Cod33D));
            cells.Add(new FastExcel.Cell( 13,item.DC_Other_Factors));
            cells.Add(new FastExcel.Cell( 14,item.ACME_UC));
            cells.Add(new FastExcel.Cell( 15,item.MAN_UC));
            cells.Add(new FastExcel.Cell( 16,item.EAC));
            cells.Add(new FastExcel.Cell( 17,item.CDC_CheckBox));
            cells.Add(new FastExcel.Cell( 18,item.CDC_ICD));
            cells.Add(new FastExcel.Cell( 19,item.CDC_LiteralCOD));
            cells.Add(new FastExcel.Cell( 20,item.CDC_Match_Det_BC));
            cells.Add(new FastExcel.Cell( 21,item.CDC_Match_Det_FDC));
            cells.Add(new FastExcel.Cell( 22,item.CDC_Match_Prob_BC));
            cells.Add(new FastExcel.Cell( 23,item.CDC_Match_Prob_FDC));
            cells.Add(new FastExcel.Cell( 24,item.VRO_Resolution_Status));
            cells.Add(new FastExcel.Cell( 25,item.VRO_Confirmation_Method_and_Additional_Notes));


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
            var total = new mmria.server.utils.VROSummaryItem();

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
            header.Add(new FastExcel.Cell(  1, "#"));
            header.Add(new FastExcel.Cell(  2,"Death_Year"));
            header.Add(new FastExcel.Cell(  3,"Jurisdiction_Abrev"));
            header.Add(new FastExcel.Cell(  4,"Jurisdiction_Name"));
            header.Add(new FastExcel.Cell(  5,"DC_AuxNo"));
            header.Add(new FastExcel.Cell(  6,"DC_FileNo"));
            header.Add(new FastExcel.Cell(  7,"DC_DOD"));
            header.Add(new FastExcel.Cell(  8,"DC_TimingOfDeath"));
            header.Add(new FastExcel.Cell(  9,"DC_Cod33A"));
            header.Add(new FastExcel.Cell( 10,"DC_Cod33B"));
            header.Add(new FastExcel.Cell( 11,"DC_Cod33C"));
            header.Add(new FastExcel.Cell( 12,"DC_Cod33D"));
            header.Add(new FastExcel.Cell( 13,"DC_Other_Factors"));
            header.Add(new FastExcel.Cell( 14,"ACME_UC"));
            header.Add(new FastExcel.Cell( 15,"MAN_UC"));
            header.Add(new FastExcel.Cell( 16,"EAC"));
            header.Add(new FastExcel.Cell( 17,"CDC_CheckBox"));
            header.Add(new FastExcel.Cell( 18,"CDC_ICD"));
            header.Add(new FastExcel.Cell( 19,"CDC_LiteralCOD"));
            header.Add(new FastExcel.Cell( 20,"CDC_Match_Det_BC"));
            header.Add(new FastExcel.Cell( 21,"CDC_Match_Det_FDC"));
            header.Add(new FastExcel.Cell( 22,"CDC_Match_Prob_BC"));
            header.Add(new FastExcel.Cell( 23,"CDC_Match_Prob_FDC"));
            header.Add(new FastExcel.Cell( 24,"VRO_Resolution_Status"));
            header.Add(new FastExcel.Cell( 25,"VRO_Confirmation_Method_and_Additional_Notes"));

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
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlVROExport.xlsx");;
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
#endif