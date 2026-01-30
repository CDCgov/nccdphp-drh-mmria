
using System;


namespace mmria.common.model.couchdb.recover_doc;


public sealed class Revision_Class
{
    public Revision_Class()
    {
        ids = new System.Collections.Generic.List<string>();
    }
    public int start { get; set; }
    public System.Collections.Generic.List<string> ids { get; set; }

    
}
public sealed class All_Revs
{
    public All_Revs(){}
    public string _id { get; set; }
    public string _rev { get; set; }

    public bool _deleted { get; set; }

    public Revision_Class _revisions { get; set; }
}

