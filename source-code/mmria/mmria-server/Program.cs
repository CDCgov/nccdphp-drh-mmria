using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Timers;
using System.Threading.Tasks;
using Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Quartz;
using Quartz.Impl;
using System.Diagnostics;
using Serilog.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Akka.Actor;
using Akka.Configuration;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


using mmria.server.extension;
using mmria.server.authentication;
using mmria.common.metadata;
using Akka.Http;
using System.Net;
using mmria.server.Controllers;
namespace mmria.server;

public sealed partial class Program
{    
    public static int Change_Sequence_Call_Count = 0;
    public static IList<DateTime> DateOfLastChange_Sequence_Call;    
    public static string Last_Change_Sequence = null;
    private static IConfiguration configuration = null;

    public static void Main(string[] args)
    {
        AppDomain currentDomain = AppDomain.CurrentDomain;
        currentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_UnhandledExceptionHandler);

        var builder = WebApplication.CreateBuilder(args);
        
        builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true); 
        configuration = builder.Configuration;
  
        try
        {
            //0. Determine configuration source (environment-based vs appsettings-based)
            bool is_environment_based = false;
            string is_env_str = configuration["mmria_settings:is_environment_based"] 
                                ?? System.Environment.GetEnvironmentVariable("is_environment_based");
            if(!string.IsNullOrWhiteSpace(is_env_str))
            {
                is_environment_based = is_env_str.ToLower() == "true" || is_env_str == "1";
            }

            // Helper function for clean configuration loading
            string GetConfig(string key, string defaultValue = null)
            {
                return is_environment_based 
                    ? System.Environment.GetEnvironmentVariable(key) ?? defaultValue
                    : configuration[$"mmria_settings:{key}"] ?? defaultValue;
            }

            Log.Information($"Configuration Mode: {(is_environment_based ? "Environment Variables" : "AppSettings")}");

            //1. Load logging configuration
            string log_directory = GetConfig("log_directory");
            if(!string.IsNullOrEmpty(log_directory))
            {
                try
                {
                    Serilog.Log.Logger = new Serilog.LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.File(Path.Combine(log_directory,"log.txt"), rollingInterval: RollingInterval.Day)
                        .CreateLogger();
                }
                catch(System.Exception)
                {
                    Serilog.Log.Logger = new Serilog.LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();
                }
            }
            else
            {
                Serilog.Log.Logger = new Serilog.LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
            }

            Program.DateOfLastChange_Sequence_Call = new List<DateTime>();
            Program.Change_Sequence_Call_Count++;
            Program.DateOfLastChange_Sequence_Call.Add(DateTime.Now);

            //2. Load all configuration values
            string web_site_url = GetConfig("web_site_url", "http://*:8080");//default is 8080, 12345 for local
            string app_instance_name = GetConfig("app_instance_name");
            
            string[] multiTenantJurisdictions = [];
            var envMultiTenant = GetConfig("multi_tenant_jurisdictions");
            if (!string.IsNullOrWhiteSpace(envMultiTenant)) 
                multiTenantJurisdictions = envMultiTenant.Split(',');
            
            string multi_tenant_shared_config_id = GetConfig("multi_tenant_shared_config_id") 
                                                ?? GetConfig("shared_config_id");
            
            string couchDbTemplateUrl = GetConfig("multi_tenant_shared_config_id_template_couchdb_url") 
                                    ?? GetConfig("couchdb_url");
            
            string timer_user_name = GetConfig("timer_user_name");
            string timer_value = GetConfig("timer_password") ?? GetConfig("timer_value");
            string cron_schedule = GetConfig("cron_schedule");
            
            bool is_schedule_enabled = GetConfig("is_schedule_enabled")?.ToLower() is "true" or "1";
            bool is_sams_enabled = GetConfig("sams_is_enabled")?.ToLower() is "true" or "1";

            // Read case lock duration in minutes (default 120 = 2 hours)
            string case_lock_minutes = GetConfig("case_lock_minutes") ?? "120";
            
            string couchdb_url = GetConfig("couchdb_url");
            string config_id = GetConfig("config_id");
            string shared_config_id = GetConfig("shared_config_id");

            //3. Log configuration (existing code)
            Log.Information("Pre Overridable Config:");
            Log.Information($"couchdb_url: {couchdb_url}");
            Log.Information($"timer_user_name: {timer_user_name}");
            Log.Information($"config_id: {config_id}");
            Log.Information($"shared_config_id: {shared_config_id}");
            Log.Information($"is_sams_enabled: {is_sams_enabled}");
            Log.Information($"case_lock_minutes: {case_lock_minutes}");
            Log.Information($"is_schedule_enabled: {is_schedule_enabled}");
            Log.Information($"multi_tenant_jurisdictions: {string.Join(",", multiTenantJurisdictions)}");
            Log.Information($"multi_tenant_shared_config_id: {multi_tenant_shared_config_id}");
            Log.Information($"multi_tenant_shared_config_id_template_couchdb_url: {couchDbTemplateUrl}");
            Log.Information("***********************\n");

            // Load multi-tenant configuration using centralized loader
            var configLoader = new mmria.common.couchdb.MultiTenantConfigurationLoader(configuration);
            
            // Create HTTP client for CouchDB during startup (uses SimpleHttpClientFactory)
            var configLoadingHttpFactory = new mmria.common.SimpleHttpClientFactory();
            var configLoadingHttpClient = new mmria.common.getset.CouchDbHttpClient(configLoadingHttpFactory);
            
            // Load all OverridableConfigurations for tenants
            var overridableConfigSets = configLoader.LoadOverridableConfigurationsAsync(
                multiTenantJurisdictions,
                couchDbTemplateUrl,
                timer_user_name,
                timer_value,
                multi_tenant_shared_config_id,
                config_id,
                configLoadingHttpClient).Result;
            
            Log.Information($"Loaded {overridableConfigSets.Count} OverridableConfiguration(s)");
            
            builder.Services.AddSingleton<List<mmria.common.couchdb.OverridableConfiguration>>(overridableConfigSets);
            builder.Services.AddSingleton<mmria.common.couchdb.OverridableConfiguration>(overridableConfigSets[0]);//temporary fix

            // Load all ConfigurationSets for tenants
            var dbConfigSets = configLoader.LoadConfigurationSetsAsync(
                multiTenantJurisdictions,
                couchDbTemplateUrl,
                timer_user_name,
                timer_value,
                config_id,
                configLoadingHttpClient).Result;
            
            Log.Information($"Loaded {dbConfigSets.Count} ConfigurationSet(s)");
            
            builder.Services.AddSingleton<List<mmria.common.couchdb.ConfigurationSet>>(dbConfigSets);
            builder.Services.AddSingleton<mmria.common.couchdb.ConfigurationSet>(dbConfigSets[0]);//temporary fix
            
            // Register IHttpClientFactory with default client configured for CouchDB
            // Connection pooling automatically handles multiple database URLs
            builder.Services.AddHttpClient("CouchDb", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(100);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                UseCookies = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 100,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });


            // Register CouchDbHttpClient as singleton (stateless, supports multiple db connections)
            builder.Services.AddSingleton<mmria.common.getset.CouchDbHttpClient>();

            // Register Account Manager components (DAL and Manager for Account feature)
            builder.Services.AddScoped<mmria.common.SharedLibraries.Account.DAL.AccountDAL>();
            builder.Services.AddScoped<mmria.common.SharedLibraries.Account.Manager.AccountManager>();
            builder.Services.AddScoped<mmria.common.SharedLibraries.ManageUsers.DAL.ManageUsersDAL>();
            builder.Services.AddScoped<mmria.common.SharedLibraries.ManageUsers.Manager.ManageUsersManager>();

            // Register Session Manager (replaces actor-based Post_Session and Record_Session_Event)
            builder.Services.AddScoped<mmria.common.SharedLibraries.Session.Manager.SessionManager>();

            // Create separate ServiceCollection for actors (following mmria.services pattern)
            var actorServiceCollection = new ServiceCollection();
            actorServiceCollection.AddSingleton<List<mmria.common.couchdb.ConfigurationSet>>(dbConfigSets);
            actorServiceCollection.AddSingleton<mmria.common.couchdb.ConfigurationSet>(dbConfigSets[0]);
            actorServiceCollection.AddSingleton<List<mmria.common.couchdb.OverridableConfiguration>>(overridableConfigSets);
            actorServiceCollection.AddSingleton<mmria.common.couchdb.OverridableConfiguration>(overridableConfigSets[0]);
            actorServiceCollection.AddSingleton<IConfiguration>(configuration);
            actorServiceCollection.AddLogging();
            
            // Add IHttpClientFactory and CouchDbHttpClient for actors
            actorServiceCollection.AddHttpClient(string.Empty, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(100);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                UseCookies = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            });
            actorServiceCollection.AddSingleton<mmria.common.getset.CouchDbHttpClient>();

            var actorServiceProvider = actorServiceCollection.BuildServiceProvider();

            //var hosted_service_prefix = new HostedServicePrefix(host_prefix);

            //builder.Services.AddSingleton<HostedServicePrefix>(hosted_service_prefix);

            //configuration["steve_api:sea_bucket_kms_key"] = DbConfigSet.name_value["steve_api:sea_bucket_kms_key"];
            //configuration["steve_api:client_name"] = DbConfigSet.name_value["steve_api:client_name"];
            //configuration["steve_api:client_secret_key"] = DbConfigSet.name_value["steve_api:client_secret_key"];
            //configuration["steve_api:base_url"] = DbConfigSet.name_value["steve_api:base_url"];
                        



            const string mmria_actor_system_name = "mmria-actor-system";
            var akka_port = "";//overridable_config.GetString("akka:port", host_prefix);
            var akka_seed_node = "";//overridable_config.GetString("akka:seed_node", host_prefix);

            if(string.IsNullOrWhiteSpace(akka_port))
                akka_port = "8081";

            if(string.IsNullOrWhiteSpace(akka_seed_node))
                akka_seed_node = $"akka.tcp://{mmria_actor_system_name}@{Dns.GetHostAddresses(Dns.GetHostName())[0]}:{akka_port}";


            var akka_ip_address = Dns.GetHostAddresses(Dns.GetHostName())[0];
            var akka_config_string = $$"""
            akka {
                    actor.provider = cluster
                    remote {
                        dot-netty.tcp {
                            port = {{akka_port}}
                            hostname = {{akka_ip_address}}
                        }
                    }
                    cluster {
                        seed-nodes = ["{{akka_seed_node.Replace("{ip_address}", akka_ip_address.ToString())}}"]
                    }
                }
            """;

            //System.Console.WriteLine(akka_config_string);
            //var config = ConfigurationFactory.ParseString(akka_config_string);
            var actorSystem = ActorSystem.Create(mmria_actor_system_name);
            
            // Get CouchDbHttpClient for actor creation
            var couchDbHttpClient = actorServiceProvider.GetRequiredService<mmria.common.getset.CouchDbHttpClient>();
            
            Log.Information($"ActorSystem: akka.tcp://{mmria_actor_system_name}@{Dns.GetHostAddresses(Dns.GetHostName())[0]}:{akka_port}");
            Log.Information($"Akka seed node: {akka_seed_node}");
            
            
            builder.Services.AddSingleton(typeof(ActorSystem), (serviceProvider) => actorSystem);

            // Register SharedLibraries services
            builder.Services.AddScoped<mmria.common.SharedLibraries.OfflineCase.DAL.OfflineCaseDAL>();
            builder.Services.AddScoped<mmria.common.SharedLibraries.Case.DAL.CaseDAL>();
            builder.Services.AddScoped<mmria.common.SharedLibraries.Session.DAL.SessionDAL>();
            builder.Services.AddScoped<mmria.common.SharedLibraries.OfflineCase.Manager.IOfflineCaseManager, mmria.common.SharedLibraries.OfflineCase.Manager.OfflineCaseManager>();
            builder.Services.AddScoped<mmria.common.SharedLibraries.Case.Manager.CaseManager>();
            
            // Register AggregateReport Manager
            builder.Services.AddScoped<mmria.common.Manager.AggregateReportManager>();

            // Register InteractiveReport Manager
            builder.Services.AddScoped<mmria.common.Manager.InteractiveReport.InteractiveReportManager>();

            ISchedulerFactory schedFact = new StdSchedulerFactory();
            Quartz.IScheduler sched = schedFact.GetScheduler().Result;

            DateTimeOffset runTime = DateBuilder.EvenMinuteDate(DateTimeOffset.UtcNow);

            
            var JobDataMap = new Quartz.JobDataMap();

            JobDataMap.Add("ActorSystem", actorSystem);
            
            IJobDetail job = JobBuilder.Create<mmria.server.model.Pulse_job>()
                .WithIdentity("job1", "group1")
                .SetJobData(JobDataMap)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .StartAt(runTime.AddMinutes(3))
                .WithCronSchedule(cron_schedule)
                .Build();

            sched.ScheduleJob(job, trigger);


            
            if (is_schedule_enabled) sched.Start();
                
            // Create QuartzSupervisor for each tenant
            for (int i = 0; i < multiTenantJurisdictions.Length; i++)
            {
                var tenant = multiTenantJurisdictions[i].Trim();

                // Skip CDC - it doesn't have standard tenant configuration structure
                if (tenant.Equals("cdc", StringComparison.OrdinalIgnoreCase) || tenant.Equals("cdcqa", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information($"Skipping QuartzSupervisor creation for CDC tenant");
                    continue;
                }
                var quartzSupervisor = actorSystem.ActorOf
                (
                    Props.Create<mmria.server.model.actor.QuartzSupervisor>
                    (
                        overridableConfigSets[i],
                        tenant,
                        dbConfigSets[i],
                        couchDbHttpClient
                    ), 
                    $"QuartzSupervisor-{tenant}"
                );
                
                quartzSupervisor.Tell("init");
                
                Log.Information($"QuartzSupervisor created for tenant: {tenant}");
            }

            actorSystem.ActorOf(Props.Create<mmria.server.SteveAPISupervisor>(), "steve-api-supervisor");

            if(is_sams_enabled){         
                Log.Information("using sams");

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CustomAuthOptions.DefaultScheme;
                    options.DefaultChallengeScheme = CustomAuthOptions.DefaultScheme;
                })
                .AddCustomAuth(options =>
                {
                    options.AuthKey = "custom auth key";
                    options.Is_SAMS = true;
                });
            }
            else
            {
                Log.Information("NOT using sams");

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CustomAuthOptions.DefaultScheme;
                    options.DefaultChallengeScheme = CustomAuthOptions.DefaultScheme;
                })
                .AddCustomAuth(options =>
                {
                    options.AuthKey = "custom auth key";
                    options.Is_SAMS = false;
                });
            }

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("abstractor", policy => policy.RequireRole("abstractor"));
                options.AddPolicy("data_analyst", policy => policy.RequireRole("data_analyst"));
                options.AddPolicy("form_designer", policy => policy.RequireRole("form_designer"));
                options.AddPolicy("committee_member", policy => policy.RequireRole("committee_member"));
                options.AddPolicy("vital_importer", policy => policy.RequireRole("vital_importer"));
                options.AddPolicy("vital_importer_state", policy => policy.RequireRole("vital_importer_state"));
                options.AddPolicy("cdc_admin", policy => policy.RequireRole("cdc_admin"));
                options.AddPolicy("cdc_analyst", policy => policy.RequireRole("cdc_analyst"));
                options.AddPolicy("jurisdiction_admin", policy => policy.RequireRole("jurisdiction_admin"));
                options.AddPolicy("installation_admin", policy => policy.RequireRole("installation_admin"));
                options.AddPolicy("guest", policy => policy.RequireRole("guest"));
                //if(is_pmss_enhanced) options.AddPolicy("vro", policy => policy.RequireRole("vro"));
            });

            builder.Services.AddMvc
            (
                config =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                                    .RequireAuthenticatedUser()
                                    .Build();
                    config.Filters.Add(new AuthorizeFilter(policy));

                    config.CacheProfiles.Add
                    (
                        "NoStore",
                        new Microsoft.AspNetCore.Mvc.CacheProfile()
                        {
                            NoStore = true
                        }
                    );
                }
            );

            // Configure Kestrel for OpenShift - support environment variables from ConfigMap
            int maxConnections = 1000;
            int maxUpgradedConnections = 1000;
            int http2MaxStreams = 100;
            int keepAliveTimeoutSeconds = 120;
            int requestHeaderTimeoutSeconds = 30;

            System.Environment.GetEnvironmentVariable("KESTREL_MAX_CONNECTIONS")?.SetIfIsNotNullOrWhiteSpace(ref maxConnections);
            System.Environment.GetEnvironmentVariable("KESTREL_MAX_UPGRADED_CONNECTIONS")?.SetIfIsNotNullOrWhiteSpace(ref maxUpgradedConnections);
            System.Environment.GetEnvironmentVariable("KESTREL_HTTP2_MAX_STREAMS")?.SetIfIsNotNullOrWhiteSpace(ref http2MaxStreams);
            System.Environment.GetEnvironmentVariable("KESTREL_KEEPALIVE_TIMEOUT")?.SetIfIsNotNullOrWhiteSpace(ref keepAliveTimeoutSeconds);
            System.Environment.GetEnvironmentVariable("KESTREL_REQUEST_HEADER_TIMEOUT")?.SetIfIsNotNullOrWhiteSpace(ref requestHeaderTimeoutSeconds);

            Log.Information("Kestrel Configuration:");
            Log.Information($"  MaxConnections: {maxConnections}");
            Log.Information($"  MaxUpgradedConnections: {maxUpgradedConnections}");
            Log.Information($"  HTTP2 MaxStreams: {http2MaxStreams}");
            Log.Information($"  KeepAlive Timeout: {keepAliveTimeoutSeconds}s");
            Log.Information($"  Request Header Timeout: {requestHeaderTimeoutSeconds}s");

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Limits.MaxConcurrentConnections = maxConnections;
                serverOptions.Limits.MaxConcurrentUpgradedConnections = maxUpgradedConnections;
                serverOptions.Limits.Http2.MaxStreamsPerConnection = http2MaxStreams;
                serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(keepAliveTimeoutSeconds);
                serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(requestHeaderTimeoutSeconds);
            });

            builder.Services.AddControllersWithViews()
                .AddNewtonsoftJson(x => 
                    {
                        //x.SerializerSettings.MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore;
                        x.SerializerSettings.Converters.Add(new mmria.common.utils.TimeOnlyJsonConverter());
                        x.SerializerSettings.Converters.Add(new mmria.common.utils.DateOnlyJsonConverter());
                        x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    });
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            if (is_schedule_enabled)
            {
                System.Threading.Tasks.Task.Run
                (
                    new Action(async () =>
                    {
                        // Setup database - handle both single and multi-tenant modes
                        if (multiTenantJurisdictions.Length == 0)
                        {
                            // Single tenant mode (backwards compatible)
                            try
                            {
                                Log.Information("Starting database setup for single tenant mode");
                                
                                await new mmria.server.utils.c_db_setup
                                (
                                    actorSystem,
                                    overridableConfigSets[0],
                                    config_id, // No tenant name in single-tenant mode
                                    couchDbHttpClient
                                ).Setup();
                                
                                Log.Information("Completed database setup for single tenant mode");
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Failed database setup for single tenant mode\n{ex}");
                            }
                        }
                        else
                        {
                            // Multi-tenant mode - setup database for each tenant sequentially
                            for (int i = 0; i < multiTenantJurisdictions.Length; i++)
                            {
                                var tenant = multiTenantJurisdictions[i].Trim();
                                try
                                {
                                    Log.Information($"Starting database setup for tenant: {tenant}");
                                    
                                    await new mmria.server.utils.c_db_setup
                                    (
                                        actorSystem,
                                        overridableConfigSets[i],
                                        tenant,
                                        couchDbHttpClient
                                    ).Setup();
                                    
                                    Log.Information($"Completed database setup for tenant: {tenant}");
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Failed database setup for tenant: {tenant}\n{ex}");
                                }
                            }
                        }
                    })
                );
            }

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();                
            }
            

            app.Use(middleware);

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAntiforgery();
            app.UseAuthorization();

            app.MapControllerRoute("Api","api/{controller}/{action}/{id?}");
            app.MapControllerRoute("default", "{controller=Home}/{action=Index}");           

            app.Run(web_site_url);

        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"MMRIA Server error: ${ex}");
        }    
    }


    static async Task middleware(HttpContext context, Func<Task> next)
    {
        var resetFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResetFeature>();
        var current_method = context.Request.Method.ToLower();
        var request_path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        const string allowedMethodsHeader = "GET, POST, PUT, DELETE";

        static bool IsClientReset(IOException ioEx)
        {
            if (ioEx is null) return false;

            if (ioEx.InnerException is System.Net.Sockets.SocketException se)
            {
                return se.SocketErrorCode is System.Net.Sockets.SocketError.ConnectionReset
                    or System.Net.Sockets.SocketError.ConnectionAborted;
            }

            return ioEx.Message != null &&
                (
                    ioEx.Message.Contains("client reset", StringComparison.OrdinalIgnoreCase) ||
                    ioEx.Message.Contains("reset the request stream", StringComparison.OrdinalIgnoreCase)
                );
        }

        // Deny HEAD globally except for the health endpoint. If the request is a HEAD
        // for /api/healthz, translate it to GET so the existing GET handler services it
        // without changing downstream code. Otherwise return 405 Method Not Allowed.
        if (current_method == "head")
        {
            if (request_path.StartsWith("/api/healthz"))
            {
                // Convert to GET so routing and controller logic execute as for GET.
                context.Request.Method = "GET";
                current_method = "get";
            }
            else
            {
                context.Response.StatusCode = 405; // Method Not Allowed
                context.Response.Headers["Allow"] = allowedMethodsHeader;
                context.Response.Headers.Append("Connection", "close");
                if (resetFeature != null) resetFeature.Reset(errorCode: 4);
                return; // short-circuit
            }
        }
        switch (current_method)
        {
            case "get":
            case "put":
            case "post":
            case "head":
            case "delete":

            if
            (
                current_method == "post" &&  
                context.Request.Headers.ContainsKey("Content-Length") &&
                context.Request.Headers["Content-Length"].Count == 1 &&
                context.Request.ContentLength.HasValue &&
                context.Request.ContentLength.Value < 0

            )
            {
                context.Response.StatusCode = 400;
                context.Response.Headers.Append("Connection", "close");
                if (resetFeature != null) resetFeature.Reset(errorCode: 4);
                break;
            }
            else if
            (
                (
                    context.Request.Headers.ContainsKey("Content-Length") &&
                    context.Request.Headers["Content-Length"].Count > 1
                ) 
                ||
                (
                    context.Request.Headers.ContainsKey("Transfer-Encoding") &&
                    context.Request.Headers["Transfer-Encoding"].Count > 1
                )
            )
            {
                context.Response.StatusCode = 400;
                context.Response.Headers.Append("Connection", "close");
                if (resetFeature != null) resetFeature.Reset(errorCode: 4);
                //context.Abort();
                //context.RequestAborted.Session
            }
            else if
            (
                context.Request.Headers.ContainsKey("Content-Length") &&
                context.Request.Headers.ContainsKey("Transfer-Encoding")
            )
            {
                context.Response.StatusCode = 400;
                context.Response.Headers.Append("Connection", "close");
                if (resetFeature != null) resetFeature.Reset(errorCode: 4);
                // context.Abort();
            }
            else if
            (
                context.Request.Headers.ContainsKey("X-HTTP-METHOD") ||
                context.Request.Headers.ContainsKey("X-HTTP-Method-Override") ||
                context.Request.Headers.ContainsKey("X-METHOD-OVERRIDE")
            )
            {
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors  'none';");
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Connection", "close");
                context.Response.StatusCode = 400;
                //resetFeature.Reset(errorCode: 4);
                //context.Abort();
            }
            else if(next is null)
            {
                context.Response.StatusCode = 400;
                context.Response.Headers.Append("Connection", "close");
                if (resetFeature != null) resetFeature.Reset(errorCode: 4);
            }
            else
            {
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("Content-Security-Policy","frame-ancestors  'none'");
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

                try
                {
                    await next();
                }
                catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
                {
                    // Client disconnected / request aborted.
                }
                catch (IOException ioEx) when (context.RequestAborted.IsCancellationRequested || IsClientReset(ioEx))
                {
                    // Client disconnected / request reset while reading request body.
                }
            }

            break;
            default:
            context.Response.StatusCode = 400;
            context.Response.Headers.Append("Connection", "close");
            if (resetFeature != null) resetFeature.Reset(errorCode: 4);
            //context.Abort();
            break;
        }    
    }

    static void AppDomain_UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args) 
    {
        Exception e = (Exception) args.ExceptionObject;
        Console.WriteLine("AppDomain_UnhandledExceptionHandler caught : " + e.Message);
    }

    static mmria.common.couchdb.ConfigurationSet GetConfiguration
    (
        string couchdb_url,
        string config_id,
        string user_name, 
        string user_value

    )
    {
        var result = new mmria.common.couchdb.ConfigurationSet();
        string request_string = null;
        var factory = new mmria.common.SimpleHttpClientFactory();
        var couchDbHttpClient = new mmria.common.getset.CouchDbHttpClient(factory);
        try
        {
            request_string = $"{couchdb_url}/configuration/{config_id}";//tenant1
            Console.WriteLine (request_string);

            string responseFromServer = couchDbHttpClient.ExecuteAsync("GET", request_string, null, user_name, user_value, "application/json").Result;
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.couchdb.ConfigurationSet> (responseFromServer);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
            Console.WriteLine (request_string);
            
        } 

        return result;
    }


    static mmria.common.couchdb.OverridableConfiguration GetOverridableConfiguration
    (
        string url,
        string user_name,
        string user_value,
        string shared_config_id
    )
    {
        var result = new mmria.common.couchdb.OverridableConfiguration();
        var factory = new mmria.common.SimpleHttpClientFactory();
        var couchDbHttpClient = new mmria.common.getset.CouchDbHttpClient(factory);
        try
        {
            string request_string = $"{url}/configuration/{shared_config_id}";//dev_cluster (showing localhost)
            string responseFromServer = couchDbHttpClient.ExecuteAsync("GET", request_string, null, user_name, user_value, "application/json").Result;
            //System.Console.WriteLine(responseFromServer);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.couchdb.OverridableConfiguration> (responseFromServer);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return result;
    }
}



