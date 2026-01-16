namespace mmria.server.SharedLibraries.Model.OfflineCase;

public sealed class OfflineCaseListResponse
{
    public OfflineCaseListResponse () 
    {
        this.rows = new System.Collections.Generic.List<OfflineCaseItem> ();
    }

    public OfflineCaseListResponse 
    (
        int p_offset,
        System.Collections.Generic.List<OfflineCaseItem> p_rows,
        int p_total_rows 
    ) 
    {
        this.offset = p_offset;
        this.rows = p_rows;
        this.total_rows = p_total_rows;
    }


    public int offset { get; set; } //": 0,
    public System.Collections.Generic.List<OfflineCaseItem> rows { get; set; }
    public int total_rows { get; set; } 
}