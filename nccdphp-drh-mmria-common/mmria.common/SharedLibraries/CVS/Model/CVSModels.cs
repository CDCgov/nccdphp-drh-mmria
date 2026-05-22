using System.Collections.Generic;

namespace mmria.common.SharedLibraries.CVS.Model;

public sealed class CVSFileStatusResult
{
    public string file_status { get; set; }
    public string updated_lat { get; set; }
    public string updated_lon { get; set; }
    public string updated_year { get; set; }
    public int? external_status_code { get; set; }
    public string external_reason_phrase { get; set; }
    public string external_error_message { get; set; }
    public bool is_valid_address { get; set; } = true;
    public bool is_valid_year { get; set; } = true;
    public byte[] PdfBytes { get; set; }
}

public sealed class CVSExternalPostResponse
{
    public bool is_success_status_code { get; set; }
    public int status_code { get; set; }
    public string reason_phrase { get; set; }
    public string content_type { get; set; }
    public string body { get; set; }
}
