using System;

namespace mmria.common.SharedLibraries.Jurisdiction.Model;

/// <summary>
/// Read-only projection returned by <see cref="IJurisdictionAuthorizationReader.GetRolesByUserIdAsync"/>.
/// Contains the fields from <c>user_role_jurisdiction</c> that are needed for authorization decisions.
/// Active-role filtering is the responsibility of callers — this type is a raw data transfer object.
/// </summary>
public sealed class JurisdictionRoleEntry
{
    public string? _id { get; init; }
    public string? jurisdiction_id { get; init; }
    public string? user_id { get; init; }
    public string? role_name { get; init; }
    public bool? is_active { get; init; }
    public DateTime? effective_start_date { get; init; }
    public DateTime? effective_end_date { get; init; }
}
