using System;
namespace mmria.common.metadata;

public sealed class Add_Attachement
{
    public Add_Attachement() {}

    public string _id { get; set; }
    public string _rev{ get; set; }
    public string doc_name{ get; set; }

    public string document_content { get; set; }
}

