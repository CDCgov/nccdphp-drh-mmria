using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace mmria.server.authentication;

public static class CustomAuthenticationBuilderExtensions
{
    // Custom authentication extension method
    public static AuthenticationBuilder AddCustomAuth(this AuthenticationBuilder builder, Action<CustomAuthOptions> configureOptions)
    {
        // Add custom authentication scheme with custom options and custom handler
        return builder.AddScheme<CustomAuthOptions, CustomAuthHandler>(CustomAuthOptions.DefaultScheme, configureOptions);
    }
}

