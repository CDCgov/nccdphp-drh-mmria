#nullable enable

using System;
using Akka.Actor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using mmria.common.SharedLibraries.MMRIARebuild.DAL;
using mmria.common.SharedLibraries.MMRIARebuild.Manager;

namespace mmria.services.vitalsimport;

public sealed class Program
{
    public static string config_web_site_url = null!;
    public static string couchdb_url = null!;
    public static string db_prefix = null!;
    public static string timer_user_name = null!;
    public static string timer_value = null!;

    public static string? central_couchdb_url = null;
    public static string? central_timer_user_name = null;
    public static string? central_timer_value = null;

    public static string? vitals_service_key = null;
    public static string config_id = null!;
    public static string? vitals_import_additional_tenants = null;

    public static ActorSystem? ActorSystem;
    public static mmria.common.couchdb.ConfigurationSet DbConfigSet = null!;

    private static IConfiguration configuration = null!;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

        configuration = builder.Configuration;
        LoadConfigurationValues();
        DbConfigSet = LoadRequiredConfigurationSet();

        builder.Services.AddControllers();
        builder.Services.AddAuthentication("BasicAuthentication")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, mmria.services.Classes.HeaderAuthenticationHandler>("BasicAuthentication", null);

        builder.Services.AddSingleton(DbConfigSet);

        builder.Services.AddHttpClient(string.Empty, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(100);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
        })
        .ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        });

        builder.Services.AddHttpClient("CouchDbRebuild", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(100);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
        .ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            UseCookies = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            MaxConnectionsPerServer = 8,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        });

        builder.Services.AddSingleton<mmria.common.getset.CouchDbHttpClient>();
        builder.Services.AddScoped<MMRIARebuildDAL>();
        builder.Services.AddScoped<MMRIARebuildManager>(serviceProvider =>
            new MMRIARebuildManager(
                serviceProvider.GetRequiredService<MMRIARebuildDAL>(),
                serviceProvider.GetRequiredService<mmria.common.getset.CouchDbHttpClient>(),
                configuration,
                serviceProvider.GetRequiredService<mmria.common.couchdb.ConfigurationSet>()));

        builder.Services.AddSingleton<ActorSystem>(serviceProvider =>
        {
            var couchDbHttpClient = serviceProvider.GetRequiredService<mmria.common.getset.CouchDbHttpClient>();
            var populateCdcThrottleSettings = mmria.services.populate_cdc_instance.PopulateCdcThrottleSettingsLoader.Load(configuration);

            Console.WriteLine($"[PopulateCDC] Copy throttling settings: {populateCdcThrottleSettings.Copy.ToLogString()}.");
            Console.WriteLine($"[PopulateCDC] Rebuild throttling settings: {populateCdcThrottleSettings.Rebuild.ToLogString()}.");

            var actorSystem = Akka.Actor.ActorSystem.Create("mmria-actor-system");
            actorSystem.ActorOf(Akka.Actor.Props.Create<RecordsProcessor_Worker.Actors.BatchSupervisor>(couchDbHttpClient), "batch-supervisor");
            actorSystem.ActorOf(Akka.Actor.Props.Create<mmria.services.backup.BackupSupervisor>(couchDbHttpClient), "backup-supervisor");
            actorSystem.ActorOf(
                Akka.Actor.Props.Create<mmria.services.populate_cdc_instance.PopulateCDCInstanceSupervisor>(
                    couchDbHttpClient,
                    populateCdcThrottleSettings),
                "populate-cdc-instance-supervisor");

            Program.ActorSystem = actorSystem;
            return actorSystem;
        });

        builder.Services.AddHostedService<Worker>();

        var app = builder.Build();
        var actorSystem = app.Services.GetRequiredService<ActorSystem>();
        Program.ActorSystem = actorSystem;

        ConfigureQuartz(actorSystem, DbConfigSet);

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        app.Run(config_web_site_url);
    }

    private static void LoadConfigurationValues()
    {
        if (bool.Parse(configuration["mmria_settings:is_environment_based"]))
        {
            config_web_site_url = System.Environment.GetEnvironmentVariable("web_site_url") ?? string.Empty;
            couchdb_url = System.Environment.GetEnvironmentVariable("couchdb_url") ?? string.Empty;
            db_prefix = System.Environment.GetEnvironmentVariable("db_prefix") ?? string.Empty;
            timer_user_name = System.Environment.GetEnvironmentVariable("timer_user_name") ?? string.Empty;
            timer_value = System.Environment.GetEnvironmentVariable("timer_password") ?? string.Empty;
            central_couchdb_url = System.Environment.GetEnvironmentVariable("central_couchdb_url");
            central_timer_user_name = System.Environment.GetEnvironmentVariable("central_timer_password");
            central_timer_value = System.Environment.GetEnvironmentVariable("central_timer_password");
            vitals_service_key = System.Environment.GetEnvironmentVariable("vitals_service_key");
            config_id = System.Environment.GetEnvironmentVariable("config_id") ?? string.Empty;
            vitals_import_additional_tenants = System.Environment.GetEnvironmentVariable("vitals_import_additional_tenants");

            configuration["mmria_settings:web_site_url"] = config_web_site_url;
            configuration["mmria_settings:couchdb_url"] = couchdb_url;
            configuration["mmria_settings:db_prefix"] = db_prefix;
            configuration["mmria_settings:timer_user_name"] = timer_user_name;
            configuration["mmria_settings:timer_value"] = timer_value;
            configuration["mmria_settings:central_couchdb_url"] = central_couchdb_url;
            configuration["mmria_settings:central_timer_password"] = central_timer_user_name;
            configuration["mmria_settings:central_timer_password"] = central_timer_value;
            configuration["mmria_settings:vitals_service_key"] = vitals_service_key;
            configuration["mmria_settings:config_id"] = config_id;
            configuration["mmria_settings:vitals_import_additional_tenants"] = vitals_import_additional_tenants;
            return;
        }

        config_web_site_url = configuration["mmria_settings:web_site_url"] ?? string.Empty;
        couchdb_url = configuration["mmria_settings:couchdb_url"] ?? string.Empty;
        db_prefix = configuration["mmria_settings:db_prefix"] ?? string.Empty;
        timer_user_name = configuration["mmria_settings:timer_user_name"] ?? string.Empty;
        timer_value = configuration["mmria_settings:timer_password"] ?? string.Empty;
        central_couchdb_url = configuration["mmria_settings:central_couchdb_url"];
        central_timer_user_name = configuration["mmria_settings:central_timer_password"];
        central_timer_value = configuration["mmria_settings:central_timer_password"];
        vitals_service_key = configuration["mmria_settings:vitals_service_key"];
        config_id = configuration["mmria_settings:config_id"] ?? string.Empty;
        vitals_import_additional_tenants = configuration["mmria_settings:vitals_import_additional_tenants"];
    }

    private static mmria.common.couchdb.ConfigurationSet LoadRequiredConfigurationSet()
    {
        var startupHttpClientFactory = new mmria.common.SimpleHttpClientFactory();
        var startupCouchDbHttpClient = new mmria.common.getset.CouchDbHttpClient(startupHttpClientFactory);
        var configLoader = new mmria.common.couchdb.MultiTenantConfigurationLoader(configuration);
        var configurationSets = configLoader.LoadRequiredConfigurationSetsAsync(
                Array.Empty<string>(),
                couchdb_url,
                timer_user_name,
                timer_value,
                config_id,
                startupCouchDbHttpClient)
            .GetAwaiter()
            .GetResult();

        if (configurationSets.Count != 1)
        {
            throw new InvalidOperationException(
                $"mmria.services startup expected exactly one ConfigurationSet for config_id '{config_id}', but loaded {configurationSets.Count}.");
        }

        return configurationSets[0];
    }

    private static void ConfigureQuartz(ActorSystem actorSystem, mmria.common.couchdb.ConfigurationSet dbConfigSet)
    {
        var quartzSupervisor = actorSystem.ActorOf(Props.Create<mmria.server.model.actor.QuartzSupervisor>(), "QuartzSupervisor");
        quartzSupervisor.Tell("init");

        ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
        Quartz.IScheduler scheduler = schedulerFactory.GetScheduler().Result;

        DateTimeOffset runTime = DateBuilder.EvenMinuteDate(DateTimeOffset.UtcNow);
        IJobDetail job = JobBuilder.Create<mmria.services.vitalsimport.Pulse_job>()
            .WithIdentity("job1", "group1")
            .Build();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity("trigger1", "group1")
            .StartAt(runTime.AddMinutes(3))
            .WithCronSchedule(GetRequiredCronSchedule(dbConfigSet))
            .Build();

        scheduler.ScheduleJob(job, trigger).GetAwaiter().GetResult();
        scheduler.Start().GetAwaiter().GetResult();
    }

    private static string GetRequiredCronSchedule(mmria.common.couchdb.ConfigurationSet dbConfigSet)
    {
        if (dbConfigSet?.name_value == null ||
            !dbConfigSet.name_value.TryGetValue("cron_schedule", out string? cronSchedule) ||
            string.IsNullOrWhiteSpace(cronSchedule))
        {
            throw new InvalidOperationException("Required cron_schedule is missing from the mmria.services ConfigurationSet.");
        }

        return cronSchedule;
    }
}

