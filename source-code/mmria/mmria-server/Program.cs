using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
//using Akka.DI.Extensions.DependencyInjection;
using Akka.Configuration;
/*
using Akka.HealthCheck.Hosting;
using Akka.HealthCheck.Hosting.Web;
using WebApiTemplate.App.Configuration;
*/

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
    public static Quartz.IScheduler sched;
    public static ITrigger check_for_changes_job_trigger;
    public static ITrigger rebuild_queue_job_trigger;

    public static Dictionary<string, string> Change_Sequence_List;
    public static int Change_Sequence_Call_Count = 0;
    public static IList<DateTime> DateOfLastChange_Sequence_Call;
    public static string Last_Change_Sequence = null;

    private static IConfiguration configuration = null;

    public static void Main(string[] args)
    {
        AppDomain currentDomain = AppDomain.CurrentDomain;
        currentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_UnhandledExceptionHandler);

        var builder = WebApplication.CreateBuilder(args);
/*
        builder.Services.AddScoped<StateContainer>();

        //builder.Services.AddHostedService<AkkaHostedService>();
        builder.Services.AddSingleton<mmria.server.metadataController>();
        builder.Services.AddSingleton<Controllers.broadcast_messageController>();
        builder.Services.AddSingleton<Controllers.data_dictionaryController>();
        builder.Services.AddSingleton<mmria.server.versionController>();
        builder.Services.AddSingleton<mmria.server.jurisdiction_treeController>();
        builder.Services.AddSingleton<user_role_jurisdiction_viewController>();
*/

        
        configuration = builder.Configuration;

        //string config_export_directory = "/workspace/export";

        try
        {
            /*
            configuration = new configurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("apLoggerConfigurationpsettings.json", true, true)
            .AddUserSecrets<Startup>()
            .Build();
            */



            if(configuration["mmria_settings:log_directory"]!= null && !string.IsNullOrEmpty(configuration["mmria_settings:log_directory"]))
            {
                try
                {
                    Serilog.Log.Logger = new Serilog.LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File(Path.Combine(configuration["mmria_settings:log_directory"],"log.txt"), rollingInterval: RollingInterval.Day)
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


            

            //configuration["mmria_settings:is_db_check_enabled"].SetIfIsNotNullOrWhiteSpace(ref Program.is_db_check_enabled);
            
            
            
            //configuration["mmria_settings:cdc_instance_pull_list"].SetIfIsNotNullOrWhiteSpace(ref Program.config_cdc_instance_pull_list);
            //configuration["mmria_settings:cdc_instance_pull_db_url"].SetIfIsNotNullOrWhiteSpace(ref Program.config_cdc_instance_pull_db_url);
            //configuration["mmria_settings:vitals_url"].SetIfIsNotNullOrWhiteSpace(ref Program.config_vitals_url);
            


            string couchdb_url =  configuration["mmria_settings:couchdb_url"];
            string timer_user_name = configuration["mmria_settings:timer_user_name"];
            string timer_value = configuration["mmria_settings:timer_value"];
            string shared_config_id = configuration["mmria_settings:shared_config_id"];
            string host_prefix = "shared";
            string config_id = null;
            string app_instance_name = null;

            bool sams_is_enabled = false;

            


            configuration["mmria_settings:config_id"].SetIfIsNotNullOrWhiteSpace(ref host_prefix);
            configuration["mmria_settings:shared_config_id"].SetIfIsNotNullOrWhiteSpace(ref shared_config_id);
            configuration["mmria_settings:app_instance_name"].SetIfIsNotNullOrWhiteSpace(ref app_instance_name);
            configuration["sams:is_enabled"].SetIfIsNotNullOrWhiteSpace(ref sams_is_enabled);

            System.Environment.GetEnvironmentVariable("couchdb_url").SetIfIsNotNullOrWhiteSpace(ref couchdb_url);
            System.Environment.GetEnvironmentVariable("timer_user_name").SetIfIsNotNullOrWhiteSpace(ref timer_user_name);
            System.Environment.GetEnvironmentVariable("timer_password").SetIfIsNotNullOrWhiteSpace(ref timer_value);
            System.Environment.GetEnvironmentVariable("shared_config_id").SetIfIsNotNullOrWhiteSpace(ref shared_config_id);
            System.Environment.GetEnvironmentVariable("config_id").SetIfIsNotNullOrWhiteSpace(ref config_id);
            System.Environment.GetEnvironmentVariable("app_instance_name").SetIfIsNotNullOrWhiteSpace(ref app_instance_name);
            System.Environment.GetEnvironmentVariable("sams_is_enabled").SetIfIsNotNullOrWhiteSpace(ref sams_is_enabled);

            if(host_prefix == "shared")
            {
                System.Environment.GetEnvironmentVariable("config_id").SetIfIsNotNullOrWhiteSpace(ref host_prefix);
            }

            //Program.config_geocode_api_key = configuration["mmria_settings:geocode_api_key"];
            //Program.config_geocode_api_url = configuration["mmria_settings:geocode_api_url"];
            


            Log.Information("Pre Overridable Config:");
            Log.Information($"couchdb_url: {couchdb_url}");
            Log.Information($"timer_user_name: {timer_user_name}");
            Log.Information($"host_prefix({host_prefix.Length}): {host_prefix}");
            Log.Information($"config_id: {config_id}");
            Log.Information($"shared_config_id: {shared_config_id}");
            Log.Information($"sams:is_enabled: {sams_is_enabled}");
            Log.Information("***********************\n");



            var overridable_config = GetOverridableConfiguration
            (
                new()
                {
                    url =  couchdb_url,
                    user_name = timer_user_name,
                    user_value = timer_value
                },
                shared_config_id
            );

            Log.Information($"loaded shared_config key list:");
            var key_set = new HashSet<string>();

            foreach(var kvp in overridable_config.boolean_keys)
            {
                key_set.Add(kvp.Key);
            }

            foreach(var kvp in overridable_config.string_keys)
            {
                key_set.Add(kvp.Key);
            }

            foreach(var kvp in overridable_config.integer_keys)
            {
                key_set.Add(kvp.Key);
            }

            foreach(var key in key_set)
            {
                Log.Information("\t" + key);
            }
            Log.Information("\n");


            overridable_config.SetString(host_prefix, "shared_config_id", shared_config_id);

            builder.Services.AddSingleton<mmria.common.couchdb.OverridableConfiguration>(overridable_config);

            if(string.IsNullOrWhiteSpace(overridable_config.GetString("config_id",host_prefix)))
            {
                if(string.IsNullOrWhiteSpace(config_id))
                {

                    overridable_config.SetString(host_prefix, "config_id", host_prefix);
                }
                else

                {
                    overridable_config.SetString(host_prefix, "config_id", config_id);
                }
                Log.Information($"*config_id = {overridable_config.GetString("config_id",host_prefix)}");
            }
            else
            {
                Log.Information($"config_id = {overridable_config.GetString("config_id",host_prefix)}");
            }


            if(string.IsNullOrWhiteSpace(overridable_config.GetOverridedString("app_instance_name",host_prefix)))
            {
                
                overridable_config.SetString(host_prefix, "app_instance_name", app_instance_name);
                Log.Information("*app_instance_name: {0}", overridable_config.GetString("app_instance_name", host_prefix));
            }
            else
            {
                Log.Information("app_instance_name: {0}", overridable_config.GetString("app_instance_name", host_prefix));
            }



            var sams_exists = overridable_config.GetBoolean("sams:is_enabled",host_prefix);
            

            if
            (
                !sams_exists.HasValue ||
                sams_exists.Value != sams_is_enabled
            )
            {
                
                if(sams_exists.HasValue)
                {
                    Log.Information("sams_exists: {0}", sams_exists.Value);
                }
                overridable_config.SetBoolean(host_prefix, "*sams:is_enabled", sams_is_enabled);
                var val = overridable_config.GetBoolean("sams:is_enabled", host_prefix);
                if(val.HasValue)
                {
                   
                    Log.Information("*sams:is_enabled: {0}", val.Value);
                }
                else
                {
                    Log.Information("*sams:is_enabled: problem with overridable_config");
                }
                
            }
            else
            {
                Log.Information("sams:is_enabled: {0}", overridable_config.GetBoolean("sams:is_enabled", host_prefix));
            }

            var is_pmss_enhanced_check = overridable_config.GetBoolean("is_pmss_enhanced",host_prefix);
            bool is_pmss_enhanced = false;

            #if IS_PMSS_ENHANCED
                is_pmss_enhanced = true;
            #endif

            overridable_config.SetBoolean(host_prefix, "is_pmss_enhanced", is_pmss_enhanced);


            Log.Information($"is_pmss_enhanced = {is_pmss_enhanced}");
            Log.Information($"host_prefix = {host_prefix}");
            Log.Information("metadata_version: {0}", overridable_config.GetString("metadata_version", host_prefix));
          
            Log.Information($"db_config.user_name = {overridable_config.GetString("timer_user_name",host_prefix)}");
            Log.Information($"db_config.url = {overridable_config.GetString("couchdb_url", host_prefix)}");
            Log.Information($"db_config.prefix = {overridable_config.GetString("db_prefix",host_prefix)}");
            
            Log.Information($"shared_config_id = {overridable_config.GetString("shared_config_id",host_prefix)}");
            Log.Information($"Logging = {configuration["Logging:IncludeScopes"]}");
            Log.Information($"Console = {configuration["Console:LogLevel:Default"]}");
            
            Log.Information("sams:callback_url: {0}", overridable_config.GetString("sams:callback_url",host_prefix));
            Log.Information("sams:activity_name: {0}", overridable_config.GetString("sams:activity_name",host_prefix));
            Log.Information("is_schedule_enabled: {0}", overridable_config.GetBoolean("is_schedule_enabled", host_prefix));
            Log.Information("is_db_check_enabled: {0}", overridable_config.GetBoolean("is_db_check_enabled", host_prefix));
            Log.Information("is_development: {0}", overridable_config.GetBoolean("is_development", host_prefix));
            Log.Information("session_idle_timeout_minutes: {0}", overridable_config.GetInteger("session_idle_timeout_minutes", host_prefix));
            Log.Information("vitals_url: {0}", overridable_config.GetString("vitals_url", host_prefix));

            if(!string.IsNullOrWhiteSpace(overridable_config.GetString("vitals_service_key", host_prefix)))
            {
                Log.Information("vitals_service_key is present");
            }

            var DbConfigSet = GetConfiguration
            (
                couchdb_url,
                overridable_config.GetString("config_id",host_prefix),
                timer_user_name,
                timer_value
            );


            builder.Services.AddSingleton<mmria.common.couchdb.ConfigurationSet>(DbConfigSet);

            //var hosted_service_prefix = new HostedServicePrefix(host_prefix);

            //builder.Services.AddSingleton<HostedServicePrefix>(hosted_service_prefix);

            configuration["steve_api:sea_bucket_kms_key"] = DbConfigSet.name_value["steve_api:sea_bucket_kms_key"];
            configuration["steve_api:client_name"] = DbConfigSet.name_value["steve_api:client_name"];
            configuration["steve_api:client_secret_key"] = DbConfigSet.name_value["steve_api:client_secret_key"];
            configuration["steve_api:base_url"] = DbConfigSet.name_value["steve_api:base_url"];
                        




            // ******* To Be removed start
            configuration["mmria_settings:config_id"] = overridable_config.GetString("config_id", host_prefix);
            configuration["mmria_settings:export_directory"] = overridable_config.GetString("export_directory", host_prefix);
            configuration["mmria_settings:metadata_version"] = overridable_config.GetString("metadata_version", host_prefix);
            configuration["mmria_settings:vitals_service_key"] = overridable_config.GetString("vitals_service_key", host_prefix);
            configuration["mmria_settings:is_schedule_enabled"] = overridable_config.GetString("is_schedule_enabled", host_prefix);
            configuration["mmria_settings:db_prefix"] = overridable_config.GetString("db_prefix", host_prefix);

            // ******* To Be removed end

            const string mmria_actor_system_name = "mmria-actor-system";
            var akka_port = overridable_config.GetString("akka:port", host_prefix);
            var akka_seed_node = overridable_config.GetString("akka:seed_node", host_prefix);

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
            //var actorSystem = ActorSystem.Create(mmria_actor_system_name, config).UseServiceProvider(provider);
            var actorSystem = ActorSystem.Create(mmria_actor_system_name);//.UseServiceProvider(provider);
            
            Log.Information($"ActorSystem: akka.tcp://{mmria_actor_system_name}@{Dns.GetHostAddresses(Dns.GetHostName())[0]}:{akka_port}");
            Log.Information($"Akka seed node: {akka_seed_node}");
            
            
            builder.Services.AddSingleton(typeof(ActorSystem), (serviceProvider) => actorSystem);

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
                .WithCronSchedule(overridable_config.GetString("cron_schedule", host_prefix))
                .Build();

            sched.ScheduleJob(job, trigger);


            var is_schedule_enabled = overridable_config.GetBoolean("is_schedule_enabled", host_prefix);
            if 
            (
                is_schedule_enabled.HasValue && 
                is_schedule_enabled.Value
            )
            {
                sched.Start();
            }

            var quartzSupervisor = actorSystem.ActorOf
            (
                Props.Create<mmria.server.model.actor.QuartzSupervisor>
                (
                    overridable_config,
                    host_prefix,
                    DbConfigSet
                ), 
                "QuartzSupervisor"
            );
            actorSystem.ActorOf(Props.Create<mmria.server.SteveAPISupervisor>(), "steve-api-supervisor");
        

            quartzSupervisor.Tell("init");

            #if IS_PMSS_ENHANCED
            actorSystem.ActorOf
            (
                Props.Create<mmria.pmss.services.vitalsimport.BatchSupervisor>
                (
                    overridable_config,
                    host_prefix
                ),
                "batch-supervisor"
            );

            #endif

            bool? use_sams = overridable_config.GetBoolean("sams:is_enabled", host_prefix);
            if 
            (
                use_sams.HasValue && 
                use_sams.Value
            )
            {
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
                if(is_pmss_enhanced) options.AddPolicy("vro", policy => policy.RequireRole("vro"));
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

            builder.Services.AddControllersWithViews()
                .AddNewtonsoftJson(x => 
                    {
                        //x.SerializerSettings.MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore;
                        x.SerializerSettings.Converters.Add(new mmria.server.utils.TimeOnlyJsonConverter());
                        x.SerializerSettings.Converters.Add(new mmria.server.utils.DateOnlyJsonConverter());
                        x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    });
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            if 
            (
                is_schedule_enabled.HasValue && 
                is_schedule_enabled.Value
            )
            {
                System.Threading.Tasks.Task.Run
                (
                    new Action(async () =>
                    {
                        await new mmria.server.utils.c_db_setup
                        (
                            actorSystem,
                            overridable_config,
                            host_prefix
                        ).Setup();
                    }
                ));
            }

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseWebAssemblyDebugging();
            }
            else
            {
                //app.UseHttpsRedirection();
            }

            app.Use(middleware);

            app.UseDefaultFiles();

            //app.MapStaticAssets();
            app.UseStaticFiles();



            app.UseRouting();
            app.UseAuthentication();
            app.UseAntiforgery();
            app.UseAuthorization();

            //app.MapControllerRoute("Api","api/{controller}/{action}/{id?}");
            app.MapControllerRoute
            (
                "default", 
                "{controller=Home}/{action=Index}"
            );
            //app.MapFallbackToPage("/_Host");

            app.Run(overridable_config.GetString("web_site_url", host_prefix));

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
                resetFeature.Reset(errorCode: 4);
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
                resetFeature.Reset(errorCode: 4);
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
                resetFeature.Reset(errorCode: 4);
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
                resetFeature.Reset(errorCode: 4);
            }
            else
            {
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("Content-Security-Policy","frame-ancestors  'none'");
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

                await next();
            }

            break;
            default:
            context.Response.StatusCode = 400;
            context.Response.Headers.Add("Connection", "close");
            resetFeature.Reset(errorCode: 4);
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
        try
        {
            request_string = $"{couchdb_url}/configuration/{config_id}";
            Console.WriteLine (request_string);

            var case_curl = new mmria.server.cURL("GET", null, request_string, null, user_name, user_value);
            string responseFromServer = case_curl.execute();
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
        mmria.common.couchdb.DBConfigurationDetail configuration,
        string shared_config_id
    )
    {
        var result = new mmria.common.couchdb.OverridableConfiguration();
        try
        {
            string request_string = $"{configuration.url}/configuration/{shared_config_id}";
            var case_curl = new mmria.server.cURL("GET", null, request_string, null, configuration.user_name, configuration.user_value);
            string responseFromServer = case_curl.execute();
            //System.Console.WriteLine(responseFromServer);
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.couchdb.OverridableConfiguration> (responseFromServer);
        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return result;
    }

    public static string SetFromIfHasValue(string @this, string that)
    {
        var result = @this;

        if (!string.IsNullOrWhiteSpace(that))
        {
            result = that;
        }

        return result;
    }

    public static int SetFromIfHasValue(int @this, string that, int defaultValue)
    {
        var result = @this;
        if (!string.IsNullOrWhiteSpace(that))
        {
            if(int.TryParse(that, out var test_int))
            {
                result = test_int;
            }
            else result = defaultValue;
        }
        else
        {
            result = defaultValue;
        }

        return result;
    }

}

