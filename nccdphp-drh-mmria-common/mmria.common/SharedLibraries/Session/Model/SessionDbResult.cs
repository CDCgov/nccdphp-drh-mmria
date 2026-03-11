using mmria.common.model.couchdb;

namespace mmria.common.SharedLibraries.Session.Model;

public sealed class SessionDbResult
{
    public session_response Response { get; set; }
}

public sealed class SessionLoginResult
{
    public login_response Response { get; set; }
}
