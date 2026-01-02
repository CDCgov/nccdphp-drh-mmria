using System;
namespace mmria.common.metadata;

public sealed class form
{
    public string prompt { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string cardinality { get; set; } 
    public node[] children { get; set; } 

    public form()
    {


    }
}


