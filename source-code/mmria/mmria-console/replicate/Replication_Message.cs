using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading.Tasks;

namespace mmria.console.replicate;

public sealed class Replication_Message
{

    public Replication_Message(){}
    public string source {get;set;}//":"http://mmria:mmria@trainingdb.mmria.org:5984/_users",
    public string target{get;set;}//":"http://tdb1.mmria.org/_users"}'
}
