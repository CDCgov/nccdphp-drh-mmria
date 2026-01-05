using System;
using System.Collections.Generic;

namespace mmria.common.niosh;

public sealed class NioshResultItem
{
    public NioshResultItem(){}

    public string Code { get;set; }
    public string Title { get;set; }
    public string Probability { get;set; }
}
public sealed class NioshResult
{
    public NioshResult()
    {
        this.Industry = new();
        this.Occupation = new();
    }

    public List<NioshResultItem> Industry { get;set; }
    public List<NioshResultItem> Occupation { get;set; }

    public string Scheme {get;set;}

    public bool is_error {get;set;} = false;
}
