using System.Collections.Generic;

namespace mmria.common.SharedLibraries.CVS.Model;

public sealed class CVSFileStatusResult
{
    public string file_status { get; set; }
    public string updated_lat { get; set; }
    public string updated_lon { get; set; }
    public string updated_year { get; set; }
    public string message { get; set; }
    public bool is_valid_address { get; set; } = true;
    public bool is_valid_year { get; set; } = true;
    public byte[] PdfBytes { get; set; }
}
