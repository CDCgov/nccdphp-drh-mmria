using System;
using System.Globalization;
using System.Text;
public record DownloadRequest
{
    public DownloadRequest(){}

    public DateTime BeginDate { get;set;}
    public DateTime EndDate { get;set;}
    public string Mailbox { get;set;}

    public string seaBucketKMSKey { get;set;}
    public string clientName { get;set;}
    public string clientSecretKey { get;set;}
    public string base_url { get;set;}

    public string file_name { get;set;}
    public string download_directory {get;set; }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(nameof(DownloadRequest));
        builder.Append(" { ");
        _ = PrintMembers(builder);
        builder.Append(" }");
        return builder.ToString();
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(BeginDate));
        builder.Append(" = ");
        builder.Append(BeginDate.ToString("O", CultureInfo.InvariantCulture));
        builder.Append(", ");
        builder.Append(nameof(EndDate));
        builder.Append(" = ");
        builder.Append(EndDate.ToString("O", CultureInfo.InvariantCulture));
        builder.Append(", ");
        builder.Append(nameof(Mailbox));
        builder.Append(" = ");
        builder.Append(Mailbox ?? "(null)");
        return true;
    }
}
