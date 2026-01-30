using System;

namespace mmria.common.ije;

public sealed class BatchItemComplete
{
    public string cdc_unique_id { get; set; }
    public bool success { get; set; }
    public string error_message { get; set; }
}
