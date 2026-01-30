using System;
namespace mmria.common.ije;

public sealed class NewIJESet_MessageDTO
{
    public string mor { get; init; }

    public string nat { get; init; }

    public string fet { get; init; }

    public string mor_file_name { get; init; }

    public string nat_file_name { get; init; }

    public string fet_file_name { get; init; }

    public string case_folder { get; init; }
}

public sealed class NewIJESet_Message
{
    public string batch_id { get; init;}
    public string mor { get; init; }

    public string nat { get; init; }

    public string fet { get; init; }

    public string mor_file_name { get; init; }

    public string nat_file_name { get; init; }

    public string fet_file_name { get; init; }

    public string case_folder { get; init; }
}
public sealed class NewIJESet_MessageResponse
{
    public string batch_id { get; set; }

    public bool ok { get; set; }

    public string detail { get; set; }
}
