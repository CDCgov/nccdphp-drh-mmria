
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

using  mmria.server.extension; 
using mmria.server.util;
namespace mmria.server.Controllers;

[Authorize(Roles = "installation_admin")]

public sealed class backupManagerController : Controller
{

    mmria.common.couchdb.OverridableConfiguration configuration;
    common.couchdb.DBConfigurationDetail db_config;
    string host_prefix = null;

    mmria.common.couchdb.ConfigurationSet ConfigDB;

    private readonly mmria.common.getset.CouchDbHttpClient _couchDbHttpClient;



    private readonly ILogger<backupManagerController> _logger;
    private readonly mmria.common.SharedLibraries.BackupAdmin.Manager.BackupAdminManager _backupAdminManager;

    public backupManagerController
    (
        ILogger<backupManagerController> logger, 
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        mmria.common.getset.CouchDbHttpClient couchDbHttpClient,
        mmria.common.SharedLibraries.BackupAdmin.Manager.BackupAdminManager backupAdminManager
    )
	{
        _logger = logger;
        ConfigDB = tenantRuntime.RequireConfigurationSet();
        _couchDbHttpClient = couchDbHttpClient;
        _backupAdminManager = backupAdminManager;

        configuration = tenantRuntime.RequireConfiguration();
        db_config = tenantRuntime.RequireDbConfig();
        host_prefix = tenantRuntime.EffectiveHostPrefix;
    }

    private static string BuildBackupServiceUrl(string configUrl, params string[] pathSegments)
    {
        var encodedSegments = string.Join("/", pathSegments.Select(Uri.EscapeDataString));
        return new Uri(new Uri(configUrl.TrimEnd('/') + "/"), $"api/backup/{encodedSegments}").AbsoluteUri;
    }

   
   [Route("backupManager")]
    public async Task<IActionResult> Index()
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        List<string> file_list = await _backupAdminManager.GetFileListAsync(config_url, ConfigDB.name_value["vital_service_key"]);

        return View(file_list);
    }

    public record RemovalListResult(List<string> file_list, int over_number_of_days);

    [Route("backupManager/RemoveFileList/{over_number_of_days}")]
    public async Task<IActionResult> RemoveFileList(int over_number_of_days)
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        List<string> file_list = await _backupAdminManager.GetRemoveFileListAsync(config_url, ConfigDB.name_value["vital_service_key"], over_number_of_days);

        return View(new RemovalListResult(file_list, over_number_of_days));
    }

    [Route("backupManager/PerformFileRemoval/{over_number_of_days}")]
    public async Task<IActionResult> PerformFileRemoval(int over_number_of_days)
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        List<string> file_list = await _backupAdminManager.PerformFileRemovalAsync(config_url, ConfigDB.name_value["vital_service_key"], over_number_of_days);

        return View(new RemovalListResult(file_list, over_number_of_days));
    }

    [Route("backupManager/SubFolderFileList/{id}")]
    public async Task<IActionResult> SubFolderFileList(string id)
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        List<string> file_list = await _backupAdminManager.GetSubFolderFileListAsync(config_url, ConfigDB.name_value["vital_service_key"], id);

        return View((id, file_list));
    }

    //[Route("backup-manager/PerformHotBackup")]
    public async Task<IActionResult>  PerformHotBackup()
    {
        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        var responseContent = await _backupAdminManager.PerformHotBackupAsync(config_url, ConfigDB.name_value["vital_service_key"]);
        //System.Console.WriteLine(responseContent);

        return Ok(responseContent);
    }

    //[Route("backup-manager/PerformColdBackup")]
    public async Task<IActionResult>  PerformColdBackup()
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        var responseContent = await _backupAdminManager.PerformColdBackupAsync(config_url, ConfigDB.name_value["vital_service_key"]);
        //System.Console.WriteLine(responseContent);

        return Ok(responseContent);
    }

    public async Task<IActionResult>  PerformCompression()
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        var responseContent = await _backupAdminManager.PerformCompressionAsync(config_url, ConfigDB.name_value["vital_service_key"]);
        //System.Console.WriteLine(responseContent);

        return Ok(responseContent);
    }

    [Route("backupManager/GetFile/{id}")]
    public async Task<IActionResult>  GetFile(string id)
    {

        var config_url = configuration.GetString("vitals_url", host_prefix).Replace("/api/Message/IJESet","");
        var export_directory = configuration.GetString("export_directory", host_prefix);
        var safeFileName = ContainedPathHelper.ValidateContainedName(id, nameof(id));
        var base_url = BuildBackupServiceUrl(config_url, "GetFile", safeFileName);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("vital-service-key",  ConfigDB.name_value["vital_service_key"]);
            using (var response = await client.GetAsync(base_url))
            {
                using (var content = response.Content)
                {
                    var file_path = ContainedPathHelper.ResolveContainedFilePath(export_directory, safeFileName);

                    await using (var fs = ContainedPathHelper.OpenContainedWriteStream(export_directory, safeFileName))
                    {
                        await response.Content.CopyToAsync(fs);
                        //await fs.FlushAsync();
                        
                    }
                            
                    if(System.IO.File.Exists(file_path))
                    {
                        byte[] fileBytes = await ReadFile(file_path);

                        System.IO.File.Delete(file_path);
                        return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, safeFileName);
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
        var export_directory = configuration.GetString("export_directory", host_prefix);
        var safeFolderName = ContainedPathHelper.ValidateContainedName(folder, nameof(folder));
        var safeFileName = ContainedPathHelper.ValidateContainedName(file_name, nameof(file_name));
        var base_url = BuildBackupServiceUrl(config_url, "GetSubFolderFile", safeFolderName, safeFileName);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("vital-service-key",  ConfigDB.name_value["vital_service_key"]);
            using (var response = await client.GetAsync(base_url))
            {
                using (var content = response.Content)
                {
                    var directory_path = ContainedPathHelper.ResolveContainedDirectoryPath(export_directory, safeFolderName);
                    var file_path = ContainedPathHelper.ResolveContainedFilePath(directory_path, safeFileName);

                    System.IO.Directory.CreateDirectory(directory_path);


                    await using (System.IO.Stream contentStream = await response.Content.ReadAsStreamAsync())
                    await using (var fileStream = ContainedPathHelper.OpenContainedWriteStream(directory_path, safeFileName))
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
                            FileDownloadName = safeFileName 
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

