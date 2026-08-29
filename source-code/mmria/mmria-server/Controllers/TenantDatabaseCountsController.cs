using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mmria.common.SharedLibraries.MMRIAServices.Manager;
using mmria.common.SharedLibraries.MMRIAServices.Model;

namespace mmria.server.Controllers;

public sealed class TenantDatabaseCountsPageModel
{
    public TenantDatabaseCountsResponse Counts { get; set; }
    public string ErrorMessage { get; set; }
    public int MmrdsWatchThreshold { get; set; }
    public int MmrdsAtOrAboveThresholdCount { get; set; }
    public int DeIdMismatchCount { get; set; }
    public int EntriesWithErrorsCount { get; set; }
    public int TotalOpenCaseCountActive { get; set; }
    public int TotalOpenCaseCountStale { get; set; }
    public List<TenantDatabaseCountEntryResponse> SortedEntries { get; set; } = new();
}

[Authorize(Roles = "installation_admin")]
public sealed class TenantDatabaseCountsController : Controller
{
    private readonly mmria.common.couchdb.OverridableConfiguration _configuration;
    private readonly string _hostPrefix;
    private readonly MMRIAServicesManager _mmriaServicesManager;

    public TenantDatabaseCountsController(
        mmria.server.util.RequestTenantRuntime tenantRuntime,
        MMRIAServicesManager mmriaServicesManager)
    {
        _configuration = tenantRuntime.RequireConfiguration();
        _hostPrefix = tenantRuntime.EffectiveHostPrefix;
        _mmriaServicesManager = mmriaServicesManager;
    }

    [HttpGet("/tenant-database-counts")]
    public async Task<IActionResult> Index()
    {
        var model = new TenantDatabaseCountsPageModel
        {
            MmrdsWatchThreshold = _configuration.GetInteger("tenant_database_counts_mmrds_watch_threshold", _hostPrefix) ?? 800
        };

        try
        {
            var counts = await _mmriaServicesManager.GetTenantDatabaseCountsFromServiceAsync(
                _configuration.GetString("vitals_url", _hostPrefix),
                _configuration.GetString("vital_service_key", _hostPrefix));

            model.Counts = counts;
            model.SortedEntries = counts?.entries?
                .OrderByDescending(item => item.mmrds_doc_count.HasValue)
                .ThenByDescending(item => item.mmrds_doc_count ?? int.MinValue)
                .ThenBy(item => item.entry_name, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<TenantDatabaseCountEntryResponse>();
            model.MmrdsAtOrAboveThresholdCount = model.SortedEntries.Count(item =>
                item.mmrds_comparable_doc_count.HasValue &&
                item.mmrds_comparable_doc_count.Value >= model.MmrdsWatchThreshold);
            model.DeIdMismatchCount = model.SortedEntries.Count(item =>
                item.de_id_delta_from_mmrds.HasValue &&
                item.de_id_delta_from_mmrds.Value != 0);
            model.EntriesWithErrorsCount = model.SortedEntries.Count(item =>
                !string.Equals(item.status, "ok", StringComparison.OrdinalIgnoreCase));
            model.TotalOpenCaseCountActive = counts?.total_open_case_count_active ?? 0;
            model.TotalOpenCaseCountStale = counts?.total_open_case_count_stale ?? 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TenantDatabaseCountsController.Index failed: {ex}");
            model.ErrorMessage = "Unable to load live tenant database counts from mmria.services.";
        }

        return View(model);
    }
}
