using System;

namespace mmria.common.Model.InteractiveReport;

/// <summary>
/// Represents simple jurisdiction access information needed by the InteractiveReportManager.
/// </summary>
public sealed class JurisdictionAccessInfo
{
    public string JurisdictionId { get; set; }
    public int ResourceRight { get; set; } // 1 = ReadCase (matches mmria.server.utils.ResourceRightEnum.ReadCase)
}
