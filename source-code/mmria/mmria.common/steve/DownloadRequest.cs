using System;    
public class DownloadRequest
{
    public DownloadRequest(){}

    public DateTime BeginDate { get;set;}
    public DateTime EndDate { get;set;}
    public string Mailbox { get;set;}

    public string seaBucketKMSKey { get;set;}
    public string clientName { get;set;}
    public string clientSecretKey { get;set;}
    public string base_url { get;set;}

}