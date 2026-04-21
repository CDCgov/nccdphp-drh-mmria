using System;
using System.Collections.Generic;

namespace mmria.common.SharedLibraries.MMRIAServices.Model;

public sealed class TenantDatabaseCountsResponse
{
    public string configuration_id { get; set; }
    public DateTime generated_utc { get; set; }
    public int total_entry_count { get; set; }
    public int ok_entry_count { get; set; }
    public int partial_error_entry_count { get; set; }
    public int error_entry_count { get; set; }
    public List<TenantDatabaseCountEntryResponse> entries { get; set; } = new();
}

public sealed class TenantDatabaseCountEntryResponse
{
    public string entry_name { get; set; }
    public int? mmrds_doc_count { get; set; }
    public int? mmrds_comparable_doc_count { get; set; }
    public int? de_id_doc_count { get; set; }
    public int? report_doc_count { get; set; }
    public int? de_id_delta_from_mmrds { get; set; }
    public decimal? report_to_mmrds_ratio { get; set; }
    public string status { get; set; }
    public string mmrds_error { get; set; }
    public string de_id_error { get; set; }
    public string report_error { get; set; }
}
