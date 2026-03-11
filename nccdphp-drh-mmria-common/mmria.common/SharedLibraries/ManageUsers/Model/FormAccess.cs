using System;
using System.Collections.Generic;

namespace mmria.common.SharedLibraries.ManageUsers.Model;

public sealed class FormAccess
{
    public string form_path { get; set; }
    public string abstractor { get; set; }
    public string data_analyst { get; set; }
    public string committee_member { get; set; }
    public string vro { get; set; }
}

public sealed class FormAccessSpecification
{
    public FormAccessSpecification()
    {
        access_list = new List<FormAccess>();
    }

    public string _id { get; set; }
    public string _rev { get; set; }
    public string data_type { get; } = "form-access-specification";
    public DateTime date_created { get; set; }
    public string created_by { get; set; }
    public DateTime date_last_updated { get; set; }
    public string last_updated_by { get; set; }
    public List<FormAccess> access_list { get; set; }
}
