using System.Dynamic;
using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.VitalImport.Model;

public sealed class VitalImportSaveResult
{
    public string Id { get; set; }
    public string SerializedDocument { get; set; }
    public document_put_response Response { get; set; }
}

public sealed class VitalImportDeleteResult
{
    public string CaseId { get; set; }
    public string DocumentJson { get; set; }
    public ExpandoObject Response { get; set; }
}
