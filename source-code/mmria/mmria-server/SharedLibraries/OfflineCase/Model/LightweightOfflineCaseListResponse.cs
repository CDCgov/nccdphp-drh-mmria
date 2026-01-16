namespace mmria.server.SharedLibraries.Model.OfflineCase;

public sealed class LightweightOfflineCaseListResponse
{
    public LightweightOfflineCaseListResponse () 
    {
        this.rows = new System.Collections.Generic.List<LightweightOfflineCaseItem> ();
    }

    public LightweightOfflineCaseListResponse 
    (
        int p_offset,
        System.Collections.Generic.List<LightweightOfflineCaseItem> p_rows,
        int p_total_rows 
    ) 
    {
        this.offset = p_offset;
        this.rows = p_rows;
        this.total_rows = p_total_rows;
    }


    public int offset { get; set; }
    public System.Collections.Generic.List<LightweightOfflineCaseItem> rows { get; set; }
    public int total_rows { get; set; } 
}