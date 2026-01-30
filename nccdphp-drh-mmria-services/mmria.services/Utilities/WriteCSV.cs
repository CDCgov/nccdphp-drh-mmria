using System;
using System.Data;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace mmria.services.Utilities;

public sealed class WriteCSV
{
    string file_name;

    public string File_Name { get { return file_name;}}
    string folder_name;

    bool is_excel = false;

    StreamWriter writer;

    DataTable table;
    public WriteCSV(string p_file_name, string p_folder_name, string p_export_directory, bool p_is_excel)
    {
        this.folder_name = System.IO.Path.Combine(p_export_directory, p_folder_name);
        this.file_name = p_file_name;

        this.is_excel = p_is_excel;

        table = new DataTable("temp_table");

        if(! is_excel)
        {
            writer = new StreamWriter(folder_name + "/" + this.file_name);
        }
    }

    public DataTable Table { get { return this.table; } }

    public void WriteHeadersToStream()
    {
        if(! is_excel)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                WriteItem(writer, table.Columns[i].Caption, true);
                if (i < table.Columns.Count - 1)
                    writer.Write(',');
                else
                    writer.Write('\n');
            }
        }
    }


    public void WriteToStream()
    {
        if(is_excel)
        {
            this.WriteToExcel();
        }
        else
        {
            this.WriteToStream(writer, false, true);
        }
    }

    private void WriteToStream(TextWriter stream, bool header, bool quoteall)
    {
        if (header)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                WriteItem(stream, table.Columns[i].Caption, quoteall);
                if (i < table.Columns.Count - 1)
                    stream.Write(',');
                else
                    stream.Write('\n');
            }
        }
        foreach (DataRow row in table.Rows)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                WriteItem(stream, row[i], quoteall);
                if (i < table.Columns.Count - 1)
                    stream.Write(',');
                else
                    stream.Write('\n');
            }
        }
        stream.Flush();
        stream.Close();
    }

    public void WriteToStream(DataRow row)
    {
        const bool quoteall = true;

        for (int i = 0; i < table.Columns.Count; i++)
        {
            WriteItem(writer, row[i], quoteall);
            if (i < table.Columns.Count - 1)
                writer.Write(',');
            else
                writer.Write('\n');
        }
    }

    public void FinishStream()
    {
        writer.Flush();
        writer.Close();
    }

    private void WriteItem(TextWriter stream, object item, bool quoteall)
    {
        if (item == null)
            return;
        string s = item.ToString();
        if (quoteall || s.IndexOfAny("\",\x0A\x0D".ToCharArray()) > -1)
            stream.Write("\"" + s.Replace("\"", "\"\"") + "\"");
        else
            stream.Write(s);
        stream.Flush();
    }

    public void WriteToExcel()
    {
        var Template_xlsx = "database-scripts/Template.xlsx";
        var Output_xlsx = System.IO.Path.Combine (folder_name, file_name.Replace(".csv",".xlsx"));

        if(Output_xlsx.StartsWith("/home/net_core_user/app/workdir/mmria-export"))
        {
            Template_xlsx = "/opt/app-root/src/source-code/mmria/mmria-server/database-scripts/Template.xlsx";
        }
        
        if(System.IO.File.Exists(Output_xlsx))
            System.IO.File.Delete(Output_xlsx);

        using (FastExcel.FastExcel fastExcel = new FastExcel.FastExcel(new System.IO.FileInfo(Template_xlsx), new System.IO.FileInfo(Output_xlsx)))
        {
            fastExcel.Write(table, "sheet1");
        }
    }

}

