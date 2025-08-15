using System;

namespace mmria.server.model.couchdb;

public sealed class c_all_docs_row
{
    public c_all_docs_row() {}
    public string id { get; set;}
    public string key { get; set;}
    public c_change rev { get; set;}
}
        
/*
{"total_rows":11,"offset":0,"rows":[
{"id":"02279162-6be3-49e4-930f-42eed7cd4706","key":"02279162-6be3-49e4-930f-42eed7cd4706","value":{"rev":"1-1e8c9c42f75d1582c7d2261230268f0a"}},
{"id":"140836d7-abed-07ff-5b84-72a9ca30b9c4","key":"140836d7-abed-07ff-5b84-72a9ca30b9c4","value":{"rev":"1-7d713a250c1dd52843724df2e909841f"}},
{"id":"2243372a-9801-155c-4098-9540daabe76c","key":"2243372a-9801-155c-4098-9540daabe76c","value":{"rev":"1-d7b3cb2bbddfa7dab44161b745ba3f2c"}},
{"id":"244da20f-41cc-4300-ad94-618004a51917","key":"244da20f-41cc-4300-ad94-618004a51917","value":{"rev":"1-3930c68b758258af365bda35aee22731"}},
{"id":"999907aa-8b73-3cfa-f13b-657beb325428","key":"999907aa-8b73-3cfa-f13b-657beb325428","value":{"rev":"1-1e3bac81a24f00755613f0f7d2604fcb"}},
{"id":"acbf75d5-9c7a-57bc-9bef-59624bac7847","key":"acbf75d5-9c7a-57bc-9bef-59624bac7847","value":{"rev":"1-6f758041b4fb6954ec5ff4a52cc57eda"}},
{"id":"b5003bc5-1ab3-4ba2-8aea-9f3717c9682a","key":"b5003bc5-1ab3-4ba2-8aea-9f3717c9682a","value":{"rev":"1-ab8dc8c5852d0e053683d64ee7c5e9ba"}},
{"id":"d0e08da8-d306-4a9a-a5ff-9f1d54702091","key":"d0e08da8-d306-4a9a-a5ff-9f1d54702091","value":{"rev":"1-bbddad634887348768fa8badc4db5ded"}},
{"id":"e28af3a7-b512-d1b4-d257-19f2fabeb14d","key":"e28af3a7-b512-d1b4-d257-19f2fabeb14d","value":{"rev":"1-a9ce1f6a0be2416e2ff06ef9adb1bd0e"}},
{"id":"e98ce2be-4446-439a-bb63-d9b4e690e3c3","key":"e98ce2be-4446-439a-bb63-d9b4e690e3c3","value":{"rev":"1-297a418df441f52109714fdc3b21bd07"}},
{"id":"f6660468-ec54-a569-9903-a6682c5881d6","key":"f6660468-ec54-a569-9903-a6682c5881d6","value":{"rev":"1-113fa14b491002aa951616627cb35562"}}
]}
*/
public sealed class c_all_docs
{
    public c_all_docs ()
    {
    }

    public int total_rows { get; set;}
    public int offset { get; set;}
    public c_all_docs_row[] rows { get; set;}
}


