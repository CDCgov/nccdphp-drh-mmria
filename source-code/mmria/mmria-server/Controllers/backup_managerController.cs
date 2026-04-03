
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

    private string GetBackupServiceBaseUrl()
    {
        var configUrl = configuration.GetString("vitals_url", host_prefix);
        if (string.IsNullOrWhiteSpace(configUrl))
        {
            throw new InvalidOperationException("The current tenant is missing vitals_url configuration.");
        }

        var backupBaseUrl = configUrl.Replace("/api/Message/IJESet", string.Empty);
        if (!Uri.TryCreate(backupBaseUrl, UriKind.Absolute, out var validatedBaseUri))
        {
            throw new InvalidOperationException("The current tenant vitals_url is not a valid absolute URI.");
        }

        if (validatedBaseUri.Scheme != Uri.UriSchemeHttp && validatedBaseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("The current tenant vitals_url must use HTTP or HTTPS.");
        }

        if (!string.IsNullOrWhiteSpace(validatedBaseUri.UserInfo) || !string.IsNullOrWhiteSpace(validatedBaseUri.Fragment))
        {
            throw new InvalidOperationException("The current tenant vitals_url must not contain user info or fragments.");
        }

        return new UriBuilder(validatedBaseUri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        }.Uri.AbsoluteUri.TrimEnd('/');
    }

    private string GetVitalServiceKey()
    {
        var sanitizedVitalServiceKey = mmria.common.getset.CouchDbHttpClient.SanitizeHeader(ConfigDB.name_value["vital_service_key"])?.Trim();
        if (string.IsNullOrWhiteSpace(sanitizedVitalServiceKey))
        {
            throw new InvalidOperationException("The current tenant is missing a valid vital_service_key configuration.");
        }

        return sanitizedVitalServiceKey;
    }

    private Uri BuildBackupServiceUri(params string[] pathSegments)
    {
        return new Uri(BuildBackupServiceUrl(GetBackupServiceBaseUrl(), pathSegments));
    }

    private HttpRequestMessage CreateBackupServiceRequest(Uri requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("vital-service-key", GetVitalServiceKey());
        return request;
    }

    private static void DeleteDirectoryIfEmpty(string directoryPath)
    {
        if (!System.IO.Directory.Exists(directoryPath))
        {
            return;
        }

        if (!System.IO.Directory.EnumerateFileSystemEntries(directoryPath).Any())
        {
            System.IO.Directory.Delete(directoryPath);
        }
    }

   
   [Route("backupManager")]
    public async Task<IActionResult> Index()
    {

        List<string> file_list = await _backupAdminManager.GetFileListAsync(GetBackupServiceBaseUrl(), GetVitalServiceKey());

        return View(file_list);
    }

    public record RemovalListResult(List<string> file_list, int over_number_of_days);

    [Route("backupManager/RemoveFileList/{over_number_of_days}")]
    public async Task<IActionResult> RemoveFileList(int over_number_of_days)
    {

        List<string> file_list = await _backupAdminManager.GetRemoveFileListAsync(GetBackupServiceBaseUrl(), GetVitalServiceKey(), over_number_of_days);

        return View(new RemovalListResult(file_list, over_number_of_days));
    }

    [Route("backupManager/PerformFileRemoval/{over_number_of_days}")]
    public async Task<IActionResult> PerformFileRemoval(int over_number_of_days)
    {

        List<string> file_list = await _backupAdminManager.PerformFileRemovalAsync(GetBackupServiceBaseUrl(), GetVitalServiceKey(), over_number_of_days);

        return View(new RemovalListResult(file_list, over_number_of_days));
    }

    [Route("backupManager/SubFolderFileList/{id}")]
    public async Task<IActionResult> SubFolderFileList(string id)
    {

        List<string> file_list = await _backupAdminManager.GetSubFolderFileListAsync(GetBackupServiceBaseUrl(), GetVitalServiceKey(), id);

        return View((id, file_list));
    }

    //[Route("backup-manager/PerformHotBackup")]
    public async Task<IActionResult>  PerformHotBackup()
    {
        var responseContent = await _backupAdminManager.PerformHotBackupAsync(GetBackupServiceBaseUrl(), GetVitalServiceKey());
        //System.Console.WriteLine(responseContent);

        return Ok(responseContent);
    }

    //[Route("backup-manager/PerformColdBackup")]
    public async Task<IActionResult>  PerformColdBackup()
    {

        var responseContent = await _backupAdminManager.PerformColdBackupAsync(GetBackupServiceBaseUrl(), GetVitalServiceKey());
        //System.Console.WriteLine(responseContent);

        return Ok(responseContent);
    }

    public async Task<IActionResult>  PerformCompression()
    {

        var responseContent = await _backupAdminManager.PerformCompressionAsync(GetBackupServiceBaseUrl(), GetVitalServiceKey());
        //System.Console.WriteLine(responseContent);

        return Ok(responseContent);
    }

    [Route("backupManager/GetFile/{id}")]
    public async Task<IActionResult>  GetFile(string id)
    {

        var export_directory = configuration.GetString("export_directory", host_prefix);
        var safeFileName = ContainedPathHelper.ValidateContainedName(id, nameof(id));
        var requestUri = BuildBackupServiceUri("GetFile", safeFileName);

        using (var client = new HttpClient())
        {
            using (var request = CreateBackupServiceRequest(requestUri))
            using (var response = await client.SendAsync(request))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode);
                }

                using (var content = response.Content)
                {
                    await using (System.IO.Stream contentStream = await response.Content.ReadAsStreamAsync())
                    await using (var fs = ContainedPathHelper.OpenContainedWriteStream(export_directory, safeFileName))
                    {
                        await contentStream.CopyToAsync(fs);
                    }
                            
                    if (ContainedPathHelper.ContainedFileExists(export_directory, safeFileName))
                    {
                        byte[] fileBytes = await ContainedPathHelper.ReadContainedFileAsync(export_directory, safeFileName);

                        ContainedPathHelper.DeleteContainedFile(export_directory, safeFileName);
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
        var export_directory = configuration.GetString("export_directory", host_prefix);
        var safeFolderName = ContainedPathHelper.ValidateContainedName(folder, nameof(folder));
        var safeFileName = ContainedPathHelper.ValidateContainedName(file_name, nameof(file_name));
        var requestUri = BuildBackupServiceUri("GetSubFolderFile", safeFolderName, safeFileName);

        using (var client = new HttpClient())
        {
            using (var request = CreateBackupServiceRequest(requestUri))
            using (var response = await client.SendAsync(request))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode);
                }

                using (var content = response.Content)
                {
                    var directory_path = ContainedPathHelper.ResolveContainedDirectoryPath(export_directory, safeFolderName);

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
                            
                    if (ContainedPathHelper.ContainedFileExists(directory_path, safeFileName))
                    {
                        byte[] fileBytes = await ContainedPathHelper.ReadContainedFileAsync(directory_path, safeFileName);
                        ContainedPathHelper.DeleteContainedFile(directory_path, safeFileName);
                        DeleteDirectoryIfEmpty(directory_path);
                        return File(fileBytes, "application/octet-stream", safeFileName);
                    }
                    else
                    {
                        return NotFound();
                    }

                }
            }
        }

    }

}

