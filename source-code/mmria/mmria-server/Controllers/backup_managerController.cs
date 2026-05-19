
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        var configuredValue = ConfigDB.name_value["vital_service_key"];
        return OutboundRequestSecurityHelper.ValidateHeaderValue(configuredValue, "vital_service_key");
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
        var safeFolderName = ContainedPathHelper.ValidateContainedName(id, nameof(id));

        List<string> file_list = await _backupAdminManager.GetSubFolderFileListAsync(GetBackupServiceBaseUrl(), GetVitalServiceKey(), safeFolderName);

        return View((safeFolderName, file_list));
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

        string safeFileName;
        try
        {
            safeFileName = ContainedPathHelper.ValidateContainedName(id, nameof(id));
        }
        catch (ArgumentException)
        {
            return NotFound();
        }

        var downloadResult = await _backupAdminManager.DownloadFileAsync(GetBackupServiceBaseUrl(), GetVitalServiceKey(), safeFileName);
        if (downloadResult.IsNotFound)
        {
            return NotFound();
        }

        if (!downloadResult.IsSuccessStatusCode)
        {
            return StatusCode(downloadResult.StatusCode);
        }

        return SafeFileDownloadResultFactory.Create(
            downloadResult.Body,
            downloadResult.ContentType,
            safeFileName,
            "backup-download.bin");
    }


    [Route("backupManager/GetSubFolderFile/{folder}/{file_name}")]
    public async Task<IActionResult> GetSubFolderFile(string folder, string file_name)
    {
        string safeFolderName;
        string safeFileName;
        try
        {
            safeFolderName = ContainedPathHelper.ValidateContainedName(folder, nameof(folder));
            safeFileName = ContainedPathHelper.ValidateContainedName(file_name, nameof(file_name));
        }
        catch (ArgumentException)
        {
            return NotFound();
        }

        var downloadResult = await _backupAdminManager.DownloadSubFolderFileAsync(GetBackupServiceBaseUrl(), GetVitalServiceKey(), safeFolderName, safeFileName);
        if (downloadResult.IsNotFound)
        {
            return NotFound();
        }

        if (!downloadResult.IsSuccessStatusCode)
        {
            return StatusCode(downloadResult.StatusCode);
        }

        return SafeFileDownloadResultFactory.Create(
            downloadResult.Body,
            downloadResult.ContentType,
            safeFileName,
            "backup-download.bin");
    }

}

