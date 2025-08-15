  
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

namespace mmria.server.authentication;

public sealed class CustomAuthOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "custom auth";
    public string Scheme => DefaultScheme;
    public StringValues AuthKey { get; set; }

    public bool Is_SAMS { get; set; }

}
