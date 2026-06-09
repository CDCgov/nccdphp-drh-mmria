using System.Dynamic;

namespace mmria.common.SharedLibraries.OverdoseReport.Model;

public sealed class OverdoseMeasureResult
{
    public ExpandoObject[] docs { get; set; }
}
