using System;

namespace mmria.common.metadata;

public sealed class BroadcastMessageItem
{
    public BroadcastMessageItem()
    {

    }

    public string title { get; set; } = "new title";
    public string body { get; set; } = "";
    public string type { get; set; } = "information";
}

public sealed class BroadcastMessage
{
    public BroadcastMessage()
    {
        draft = new();
        published = new();
    }
    public BroadcastMessageItem draft { get; set; }
    public BroadcastMessageItem published { get; set; }
    
    public int publish_status { get; set; } = 0;
    
    
}

public sealed class BroadcastMessageList
{
    public BroadcastMessageList()
    {
        message_one = new();
        message_two = new();
    }
   public string _id {get; } = "broadcast-message-list";
    public string _rev{ get; set; }
    public DateTime? date_created{ get; set; }
    public string created_by{ get; set; }
    public DateTime? date_last_updated{ get; set; }
    public string last_updated_by{ get; set; }
    public string data_type { get; set; } = "broadcast_message_list";
    public BroadcastMessage message_one { get; set; }
    public BroadcastMessage message_two { get; set; }
}