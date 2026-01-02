using System;

namespace mmria.common.model.couchdb;
public sealed class replication_request
{
    public replication_request(){}
    public string source {get;set;}
    public string target {get;set;}
}
