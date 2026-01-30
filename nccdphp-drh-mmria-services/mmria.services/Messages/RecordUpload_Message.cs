using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mmria.services.vitalsimport.Messages;

public sealed class RecordUpload_Message
{
    //"eventMessage": "Record Uploaded",
    //"location": "some path here",
    //"filenames": ["filename1", "filename2"]

    public string eventMessage { get { return "Record Uploaded"; } }

    public string location { get; set; }

    public string[] filenames { get; set; }

}
    

