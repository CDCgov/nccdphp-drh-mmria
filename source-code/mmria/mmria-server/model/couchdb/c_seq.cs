using System;

namespace mmria.server.model.couchdb;

public sealed class c_change
{
    public string rev { get; set;}
}
public sealed class c_seq
{
    public string seq { get; set;}
    public string id  { get; set;}
    public bool? deleted  { get; set;}

    public System.Collections.Generic.List<c_change> changes { get; set; }
    //public List<string changes
    //{"seq":216,"id":"02279162-6be3-49e4-930f-42eed7cd4706","changes":[{"rev":"44-ed1f83ba14c23de63b90246863c2bc79"}]}
    public c_seq ()
    {
        changes = new System.Collections.Generic.List<c_change> ();
    }
}

public sealed class c_change_result
{
    public System.Collections.Generic.List<c_seq> results { get; set; }
    public string last_seq  { get; set;}
    public string  pending { get; set;}

    public c_change_result()
    {
        results = new System.Collections.Generic.List<c_seq> ();
    }
}


