using System;
namespace mmria.common.metadata;

public sealed class app
{
    public string _id { get; set; }
    public string _rev { get; set; }
    public string name { get; set; } = "mmria";
    public string prompt { get; set; } = "mmria app (ned)";
    public string type { get; set; } = "app";

    public string version { get; set; }
    public string date_created { get; set; } 
    public string created_by { get; set; } 
    public string date_last_updated { get; set; } 
    public string last_updated_by { get; set; } 
    public node[] lookup { get; set; } 

    public System.Collections.Generic.Dictionary<string,Attachment_Item> _attachments { get; set; } 

    //public System.Dynamic.ExpandoObject validation { get; set; } 
    //public System.Dynamic.ExpandoObject global { get; set; } 

    public node[] children { get; set; } 

    public app()
    {
        _attachments = new();

    }

    public node AsNode()
    {
        return new node()
        {
            name = this.name,
            prompt = this.prompt,
            type = this.type
        };
    }
}


