namespace mmria.common.steve;

public sealed class Mailbox
{
    public Mailbox()
    {
        
    }

    public string mailboxId { get; set; }
    public string listName { get; set; }
    public string fileType { get; set; }
    public bool? autoRouting { get; set; }
    public string routingCode { get; set; }
}

public sealed class GetMailboxListResult
{
    public GetMailboxListResult()
    {

    }

    public bool? success { get; set; }
    public string message {get;set;}
    public string apiVersion {get;set;}  
    public Mailbox[] mailboxes {get;set;}

}