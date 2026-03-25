using System.Collections.Generic;

namespace mmria.common.SharedLibraries.MMRIARebuild.Model;

public sealed class MMRIARebuildRequest
{
    public string tenant { get; set; }
    public string source { get; set; }
}

public sealed class MMRIARebuildResponse
{
    public bool success { get; set; }
    public int status_code { get; set; }
    public string tenant { get; set; }
    public string source { get; set; }
    public string message { get; set; }
    public string error { get; set; }
    public bool rebuild_started { get; set; }
}

public sealed class TenantRebuildReservationSnapshot
{
    public string tenant { get; init; }
    public string source { get; init; }
    public string mode { get; init; }
    public string status { get; init; }
    public string requested_utc { get; init; }
}

public sealed class StartupRebuildTenantSummary
{
    public string host_prefix { get; set; }
    public string couchdb_url { get; set; }
    public string status { get; set; }
    public string metadata_version { get; set; }
    public string last_processed_id { get; set; }
    public int completed_batch_count { get; set; }
    public int processed_case_count { get; set; }
    public int skipped_case_count { get; set; }
    public int document_error_count { get; set; }
    public int de_id_bulk_error_count { get; set; }
    public int report_bulk_error_count { get; set; }
    public int total_de_id_doc_count { get; set; }
    public int total_report_doc_count { get; set; }
    public string started_utc { get; set; }
    public string last_updated_utc { get; set; }
    public string completed_utc { get; set; }
    public string last_error { get; set; }
}

public sealed class StartupRunSummary
{
    public string _id { get; set; } = "startup-run-summary";
    public string _rev { get; set; }
    public string status { get; set; }
    public string metadata_version { get; set; }
    public string summary_host_prefix { get; set; }
    public List<string> configured_tenants { get; set; } = new();
    public Dictionary<string, StartupRebuildTenantSummary> tenant_statuses { get; set; } =
        new(System.StringComparer.OrdinalIgnoreCase);
    public int total_tenant_count { get; set; }
    public int completed_tenant_count { get; set; }
    public int paused_tenant_count { get; set; }
    public int running_tenant_count { get; set; }
    public int pending_tenant_count { get; set; }
    public int total_processed_case_count { get; set; }
    public int total_skipped_case_count { get; set; }
    public int total_document_error_count { get; set; }
    public int total_de_id_bulk_error_count { get; set; }
    public int total_report_bulk_error_count { get; set; }
    public int total_de_id_doc_count { get; set; }
    public int total_report_doc_count { get; set; }
    public string started_utc { get; set; }
    public string last_updated_utc { get; set; }
    public string completed_utc { get; set; }
    public string last_error { get; set; }
}
