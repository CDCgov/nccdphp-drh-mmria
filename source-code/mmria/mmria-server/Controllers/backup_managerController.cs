
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using  mmria.server.extension; 
namespace mmria.server.Controllers;

[Authorize(Roles = "installation_admin")]

public sealed class backupManagerController : Controller
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    mmria.common.couchdb.ConfigurationSet ConfigDB;



    private readonly ILogger<backupManagerController> _logger;

    public backupManagerController
    (
        ILogger<backupManagerController> logger, 
        mmria.common.couchdb.ConfigurationSet p_config_db,
        IHttpContextAccessor httpContextAccessor, 
        mmria.common.couchdb.OverridableConfiguration _configuration
    )
	{
        _logger = logger;
        ConfigDB = p_config_db;

        configuration = _configuration;
        host_prefix = httpContextAccessor.HttpContext.Request.Host.GetPrefix();
        db_config = configuration.GetDBConfig(host_prefix);
    }

   
   [Route("backupManager")]
    public async Task<IActionResult> Index()
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");

        var base_url = $"{config_url}/api/backup/GetFileList";


        var server_statu_curl = new mmria.server.cURL("GET", null, base_url, null);
        server_statu_curl.AddHeader("vital-service-key", ConfigDB.name_value["vital_service_key"]);

        var responseContent = await server_statu_curl.executeAsync();

        List<string> file_list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(responseContent);

        return View(file_list);
    }

    public record RemovalListResult(List<string> file_list, int over_number_of_days);

    [Route("backupManager/RemoveFileList/{over_number_of_days}")]
    public async Task<IActionResult> RemoveFileList(int over_number_of_days)
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");

        var base_url = $"{config_url}/api/backup/GetRemoveFileList/{over_number_of_days}";


        var server_statu_curl = new mmria.server.cURL("GET", null, base_url, null);
        server_statu_curl.AddHeader("vital-service-key", ConfigDB.name_value["vital_service_key"]);

        var responseContent = await server_statu_curl.executeAsync();

        List<string> file_list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(responseContent);

        return View(new RemovalListResult(file_list, over_number_of_days));
    }

    [Route("backupManager/PerformFileRemoval/{over_number_of_days}")]
    public async Task<IActionResult> PerformFileRemoval(int over_number_of_days)
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");

        var base_url = $"{config_url}/api/backup/RemoveFiles/{over_number_of_days}";


        var server_statu_curl = new mmria.server.cURL("GET", null, base_url, null);
        server_statu_curl.AddHeader("vital-service-key", ConfigDB.name_value["vital_service_key"]);

        var responseContent = await server_statu_curl.executeAsync();

        List<string> file_list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(responseContent);

        return View(new RemovalListResult(file_list, over_number_of_days));
    }

    [Route("backupManager/SubFolderFileList/{id}")]
    public async Task<IActionResult> SubFolderFileList(string id)
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");

        var base_url = $"{config_url}/api/backup/GetSubFolderFileList/{id}";

        var server_statu_curl = new mmria.server.cURL("GET", null, base_url, null);
        server_statu_curl.AddHeader("vital-service-key", ConfigDB.name_value["vital_service_key"]);

        var responseContent = await server_statu_curl.executeAsync();

        List<string> file_list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(responseContent);

        return View((id, file_list));
    }

    //[Route("backup-manager/PerformHotBackup")]
    public async Task<IActionResult>  PerformHotBackup()
    {
        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");

        var base_url = $"{config_url}/api/backup/PerformHotBackup";


        var server_statu_curl = new mmria.server.cURL("GET", null, base_url, null);
        server_statu_curl.AddHeader("vital-service-key", ConfigDB.name_value["vital_service_key"]);

        var responseContent = await server_statu_curl.executeAsync();
        //System.Console.WriteLine(responseContent);

        return Ok(responseContent);
    }

    //[Route("backup-manager/PerformColdBackup")]
    public async Task<IActionResult>  PerformColdBackup()
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        var base_url = $"{config_url}/api/backup/PerformColdBackup";

        var server_statu_curl = new mmria.server.cURL("GET", null, base_url, null);
        server_statu_curl.AddHeader("vital-service-key",  ConfigDB.name_value["vital_service_key"]);

        var responseContent = await server_statu_curl.executeAsync();
        //System.Console.WriteLine(responseContent);

        return Ok(responseContent);
    }

    public async Task<IActionResult>  PerformCompression()
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        var base_url = $"{config_url}/api/backup/PerformCompression";

        var server_statu_curl = new mmria.server.cURL("GET", null, base_url, null);
        server_statu_curl.AddHeader("vital-service-key",  ConfigDB.name_value["vital_service_key"]);

        var responseContent = await server_statu_curl.executeAsync();
        //System.Console.WriteLine(responseContent);

        return Ok(responseContent);
    }

    [Route("backupManager/GetFile/{id}")]
    public async Task<IActionResult>  GetFile(string id)
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        var base_url = $"{config_url}/api/backup/GetFile/{id}";

        var server_statu_curl = new mmria.server.cURL("GET", null, base_url, null);
        server_statu_curl.AddHeader("vital-service-key",  ConfigDB.name_value["vital_service_key"]);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("vital-service-key",  ConfigDB.name_value["vital_service_key"]);
            using (var response = await client.GetAsync(base_url))
            {
                using (var content = response.Content)
                {
                    var file_path = System.IO.Path.Combine(configuration.GetString("export_directory", host_prefix), id);

                    using (var fs = new FileStream(file_path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                        //await fs.FlushAsync();
                        
                    }
                            
                    if(System.IO.File.Exists(file_path))
                    {
                        byte[] fileBytes = await ReadFile(file_path);

                        System.IO.File.Delete(file_path);
                        return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, id);
                    }
                    else
                    {
                        return NotFound();
                    }

                }
            }
        }

    }


    [Route("backupManager/GetSubFolderFile/{folder}/{file_name}")]
    public async Task<IActionResult> GetSubFolderFile(string folder, string file_name)
    {
        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        var base_url = $"{config_url}/api/backup/GetSubFolderFile/{folder}/{file_name}";

        var server_statu_curl = new mmria.server.cURL("GET", null, base_url, null);
        server_statu_curl.AddHeader("vital-service-key",  ConfigDB.name_value["vital_service_key"]);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("vital-service-key",  ConfigDB.name_value["vital_service_key"]);
            using (var response = await client.GetAsync(base_url))
            {
                using (var content = response.Content)
                {
                    var directory_path = System.IO.Path.Combine(configuration.GetString("export_directory", host_prefix), folder);
                    var file_path = System.IO.Path.Combine(configuration.GetString("export_directory", host_prefix), folder, file_name);

                    System.IO.Directory.CreateDirectory(directory_path);


                    using (System.IO.Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new System.IO.FileStream(file_path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 8192, true))
                    {
                        const int number_of_bytes = 8192;

                        var buffer = new byte[number_of_bytes];
                        var isMoreToRead = true;

                        do
                        {
                            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);
                            }
                        }
                        while (isMoreToRead);
                    }
                            
                    if(System.IO.File.Exists(file_path))
                    {
                        return new PhysicalFileResult
                        (
                            file_path, 
                            "application/octet-stream"
                        ) 
                        { 
                            FileDownloadName = file_name 
                        };
                    }
                    else
                    {
                        return NotFound();
                    }

                }
            }
        }

    }

    async Task<byte[]> ReadFile(string s)
    {
        byte[] data;
        int br;
        int fs_length;

        using(FileStream fs = new FileStream (s, FileMode.Open, FileAccess.Read))
        {
            fs_length = (int) fs.Length;
            data = new byte[fs.Length];
            br = await fs.ReadAsync(data, 0, data.Length);
        }
        if (br != (int) fs_length)
            throw new System.IO.IOException(s);
        return data;
    }

}

