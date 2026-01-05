namespace mmria.common.steve;
public sealed class Message
{
    public Message(){}

    public string messageId {get;set;}
    public string fileName {get;set;}
    public string status {get;set;}
}

public sealed class MailBoxMessageResult
{
    public MailBoxMessageResult() {}
    public Message[] messages {get;set;}
    public int? maxCount {get;set;}
    public string apiVersion {get;set;}
    public string mailboxId {get;set;}
    public bool? success {get;set;}
    public string message {get;set;}
}

public sealed class MarkAsReadResult
{
    public MarkAsReadResult() {}
    public bool success {get;set;}
    public string message {get;set;}
    public string apiVersion {get;set;}
    public string messageId {get;set;}
}
