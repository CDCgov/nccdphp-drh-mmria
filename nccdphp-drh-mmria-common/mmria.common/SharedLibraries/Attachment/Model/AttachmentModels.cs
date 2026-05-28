using System.Collections.Generic;

namespace mmria.common.SharedLibraries.Attachment.Model;

public sealed class CentralUploadResolutionResult
{
    public CentralUploadResolutionResult()
    {
        ResultMessages = new List<string>();
        PmssNoToId = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        IsValidList = new List<bool>();
    }

    public bool IsRejectBatch { get; set; }
    public List<string> ResultMessages { get; set; }
    public Dictionary<string, string> PmssNoToId { get; set; }
    public List<bool> IsValidList { get; set; }
}
