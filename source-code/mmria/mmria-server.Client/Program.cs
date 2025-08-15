using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using mmria.common.couchdb;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
builder.Services.AddSingleton<mmria.common.couchdb.OverridableConfiguration>(new mmria.common.couchdb.OverridableConfiguration());


//builder.Configuration["BaseAddress"] = builder.HostEnvironment.BaseAddress;

var configuration = builder.Configuration;

builder.Services.AddSingleton<StateContainer>();

builder.Services
    .AddTransient<CookieHandler>()
    .AddScoped(sp => sp
        .GetRequiredService<IHttpClientFactory>()
        .CreateClient("API"))
    .AddHttpClient("API", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)).AddHttpMessageHandler<CookieHandler>();

builder.Services.AddScoped<CookieStorageAccessor>();
builder.Services.AddSingleton<IConfiguration>(configuration);


await builder.Build().RunAsync();



