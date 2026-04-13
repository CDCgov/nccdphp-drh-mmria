using System;

namespace mmria.server.util;

public sealed class RootRuntimeSettings
{
    public bool IsEnvironmentBased { get; init; }
    public bool IsMultiTenantMode { get; init; }
    public string[] ConfiguredTenants { get; init; } = Array.Empty<string>();
    public bool StartupDbRebuildEnabled { get; init; }
    public string[] StartupRebuildTenants { get; init; } = Array.Empty<string>();
    public string? SharedConfigId { get; init; }
    public string? TemplateCouchDbUrl { get; init; }
    public string? MultiTenantRebuildSource { get; init; }
    public string? SingleTenantName { get; init; }
}
