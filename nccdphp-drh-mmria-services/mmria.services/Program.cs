#nullable enable

using System;
using System.Collections.Generic;
using Akka.Actor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using mmria.common.SharedLibraries.MMRIAServices.DAL;
using mmria.common.SharedLibraries.MMRIAServices.Manager;
using mmria.common.SharedLibraries.MMRIARebuild.DAL;
using mmria.common.SharedLibraries.MMRIARebuild.Manager;

namespace mmria.services.vitalsimport;

public sealed class Program
{
    private const string HotBackupType = "hot";
    private const string ColdBackupType = "cold";
    private const string HotBackupEnabledKey = "hot_backup_enabled";
    private const string HotBackupCronScheduleKey = "hot_backup_cron_schedule";
    private const string ColdBackupEnabledKey = "cold_backup_enabled";
    private const string ColdBackupCronScheduleKey = "cold_backup_cron_schedule";
    private const string BackupCronTimeZoneKey = "backup_cron_timezone";
    private const string LegacyCronScheduleKey = "cron_schedule";
    private const string StartupRebuildIndexRestoreModeKey = "startup_rebuild_index_restore_mode";
    private const string StartupRebuildIndexWarmDelayMsKey = "startup_rebuild_index_warm_delay_ms";
    private const string StartupRebuildIndexWarmPollDelayMsKey = "startup_rebuild_index_warm_poll_delay_ms";
    private const string StartupRebuildIndexWarmTimeoutMsKey = "startup_rebuild_index_warm_timeout_ms";
    private const string StartupRebuildIndexWarmMaxSurfacesPerRunKey = "startup_rebuild_index_warm_max_surfaces_per_run";

    public static string config_web_site_url = null!;
    public static string couchdb_url = null!;
    public static string db_prefix = null!;
    public static string timer_user_name = null!;
    public static string timer_value = null!;
    public static string cron_schedule = null!;
    public static string? hot_backup_enabled = null;
    public static string? hot_backup_cron_schedule = null;
    public static string? cold_backup_enabled = null;
    public static string? cold_backup_cron_schedule = null;
    public static string? backup_cron_timezone = null;
    public static string? startup_rebuild_index_restore_mode = null;
    public static string? startup_rebuild_index_warm_delay_ms = null;
    public static string? startup_rebuild_index_warm_poll_delay_ms = null;
    public static string? startup_rebuild_index_warm_timeout_ms = null;
    public static string? startup_rebuild_index_warm_max_surfaces_per_run = null;

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
        ApplyDatabaseConfigurationValues(DbConfigSet);

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
        builder.Services.AddScoped<mmria.common.SharedLibraries.VitalImport.DAL.VitalImportDAL>();
        builder.Services.AddScoped<mmria.common.SharedLibraries.VitalImport.Manager.VitalImportManager>();
        builder.Services.AddScoped<mmria.common.SharedLibraries.ExportQueue.DAL.ExportQueueDAL>();
        builder.Services.AddScoped<mmria.common.SharedLibraries.ExportQueue.Manager.ExportQueueManager>();
        builder.Services.AddScoped<MMRIAServicesDAL>();
        builder.Services.AddScoped<MMRIAServicesManager>();
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
        builder.Services.AddHostedService<ExportQueueRetryWorker>();

        var app = builder.Build();
        var actorSystem = app.Services.GetRequiredService<ActorSystem>();
        Program.ActorSystem = actorSystem;

        ConfigureQuartz(actorSystem);

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
            cron_schedule = System.Environment.GetEnvironmentVariable(LegacyCronScheduleKey) ?? string.Empty;
            hot_backup_enabled = System.Environment.GetEnvironmentVariable(HotBackupEnabledKey);
            hot_backup_cron_schedule = System.Environment.GetEnvironmentVariable(HotBackupCronScheduleKey);
            cold_backup_enabled = System.Environment.GetEnvironmentVariable(ColdBackupEnabledKey);
            cold_backup_cron_schedule = System.Environment.GetEnvironmentVariable(ColdBackupCronScheduleKey);
            backup_cron_timezone = System.Environment.GetEnvironmentVariable(BackupCronTimeZoneKey);
            startup_rebuild_index_restore_mode = System.Environment.GetEnvironmentVariable(StartupRebuildIndexRestoreModeKey);
            startup_rebuild_index_warm_delay_ms = System.Environment.GetEnvironmentVariable(StartupRebuildIndexWarmDelayMsKey);
            startup_rebuild_index_warm_poll_delay_ms = System.Environment.GetEnvironmentVariable(StartupRebuildIndexWarmPollDelayMsKey);
            startup_rebuild_index_warm_timeout_ms = System.Environment.GetEnvironmentVariable(StartupRebuildIndexWarmTimeoutMsKey);
            startup_rebuild_index_warm_max_surfaces_per_run = System.Environment.GetEnvironmentVariable(StartupRebuildIndexWarmMaxSurfacesPerRunKey);
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
            configuration[$"mmria_settings:{LegacyCronScheduleKey}"] = cron_schedule;
            configuration[$"mmria_settings:{HotBackupEnabledKey}"] = hot_backup_enabled;
            configuration[$"mmria_settings:{HotBackupCronScheduleKey}"] = hot_backup_cron_schedule;
            configuration[$"mmria_settings:{ColdBackupEnabledKey}"] = cold_backup_enabled;
            configuration[$"mmria_settings:{ColdBackupCronScheduleKey}"] = cold_backup_cron_schedule;
            configuration[$"mmria_settings:{BackupCronTimeZoneKey}"] = backup_cron_timezone;
            configuration[$"mmria_settings:{StartupRebuildIndexRestoreModeKey}"] = startup_rebuild_index_restore_mode;
            configuration[$"mmria_settings:{StartupRebuildIndexWarmDelayMsKey}"] = startup_rebuild_index_warm_delay_ms;
            configuration[$"mmria_settings:{StartupRebuildIndexWarmPollDelayMsKey}"] = startup_rebuild_index_warm_poll_delay_ms;
            configuration[$"mmria_settings:{StartupRebuildIndexWarmTimeoutMsKey}"] = startup_rebuild_index_warm_timeout_ms;
            configuration[$"mmria_settings:{StartupRebuildIndexWarmMaxSurfacesPerRunKey}"] = startup_rebuild_index_warm_max_surfaces_per_run;
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
        cron_schedule = configuration[$"mmria_settings:{LegacyCronScheduleKey}"] ?? string.Empty;
        hot_backup_enabled = configuration[$"mmria_settings:{HotBackupEnabledKey}"];
        hot_backup_cron_schedule = configuration[$"mmria_settings:{HotBackupCronScheduleKey}"];
        cold_backup_enabled = configuration[$"mmria_settings:{ColdBackupEnabledKey}"];
        cold_backup_cron_schedule = configuration[$"mmria_settings:{ColdBackupCronScheduleKey}"];
        backup_cron_timezone = configuration[$"mmria_settings:{BackupCronTimeZoneKey}"];
        startup_rebuild_index_restore_mode = configuration[$"mmria_settings:{StartupRebuildIndexRestoreModeKey}"];
        startup_rebuild_index_warm_delay_ms = configuration[$"mmria_settings:{StartupRebuildIndexWarmDelayMsKey}"];
        startup_rebuild_index_warm_poll_delay_ms = configuration[$"mmria_settings:{StartupRebuildIndexWarmPollDelayMsKey}"];
        startup_rebuild_index_warm_timeout_ms = configuration[$"mmria_settings:{StartupRebuildIndexWarmTimeoutMsKey}"];
        startup_rebuild_index_warm_max_surfaces_per_run = configuration[$"mmria_settings:{StartupRebuildIndexWarmMaxSurfacesPerRunKey}"];
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

    private static void ApplyDatabaseConfigurationValues(mmria.common.couchdb.ConfigurationSet configurationSet)
    {
        if(configurationSet?.name_value == null)
        {
            return;
        }

        bool environmentBased = bool.TryParse(configuration["mmria_settings:is_environment_based"], out bool parsedValue) && parsedValue;

        cron_schedule = GetDatabaseConfigValue(configurationSet, LegacyCronScheduleKey, cron_schedule, environmentBased) ?? string.Empty;
        hot_backup_enabled = GetDatabaseConfigValue(configurationSet, HotBackupEnabledKey, hot_backup_enabled, environmentBased);
        hot_backup_cron_schedule = GetDatabaseConfigValue(configurationSet, HotBackupCronScheduleKey, hot_backup_cron_schedule, environmentBased);
        cold_backup_enabled = GetDatabaseConfigValue(configurationSet, ColdBackupEnabledKey, cold_backup_enabled, environmentBased);
        cold_backup_cron_schedule = GetDatabaseConfigValue(configurationSet, ColdBackupCronScheduleKey, cold_backup_cron_schedule, environmentBased);
        backup_cron_timezone = GetDatabaseConfigValue(configurationSet, BackupCronTimeZoneKey, backup_cron_timezone, environmentBased);

        configuration[$"mmria_settings:{LegacyCronScheduleKey}"] = cron_schedule;
        configuration[$"mmria_settings:{HotBackupEnabledKey}"] = hot_backup_enabled;
        configuration[$"mmria_settings:{HotBackupCronScheduleKey}"] = hot_backup_cron_schedule;
        configuration[$"mmria_settings:{ColdBackupEnabledKey}"] = cold_backup_enabled;
        configuration[$"mmria_settings:{ColdBackupCronScheduleKey}"] = cold_backup_cron_schedule;
        configuration[$"mmria_settings:{BackupCronTimeZoneKey}"] = backup_cron_timezone;
    }

    private static string? GetDatabaseConfigValue(
        mmria.common.couchdb.ConfigurationSet configurationSet,
        string key,
        string? fallback,
        bool keepExistingValue)
    {
        if(keepExistingValue && !string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        return configurationSet.name_value.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;
    }

    private static void ConfigureQuartz(ActorSystem actorSystem)
    {
        var quartzSupervisor = actorSystem.ActorOf(Props.Create<mmria.server.model.actor.QuartzSupervisor>(), "QuartzSupervisor");
        quartzSupervisor.Tell("init");

        var backupScheduleConfigurations = GetBackupScheduleConfigurations();

        ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
        Quartz.IScheduler scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();

        int scheduledJobCount = 0;
        foreach (var backupScheduleConfiguration in backupScheduleConfigurations)
        {
            if (!backupScheduleConfiguration.Enabled)
            {
                Console.WriteLine($"[BackupSchedule] {backupScheduleConfiguration.BackupType} automatic backup disabled by {backupScheduleConfiguration.EnabledKey}.");
                continue;
            }

            ScheduleBackupJob(scheduler, backupScheduleConfiguration);
            scheduledJobCount++;
        }

        if (scheduledJobCount > 0)
        {
            scheduler.Start().GetAwaiter().GetResult();
            return;
        }

        Console.WriteLine("[BackupSchedule] Quartz scheduler was configured with no automatic backup jobs because hot and cold backups are disabled.");
    }

    private static IReadOnlyList<BackupScheduleConfiguration> GetBackupScheduleConfigurations()
    {
        var nameValue = GetBackupScheduleSettings();
        var backupTimeZone = GetBackupTimeZone(nameValue);

        return new[]
        {
            GetBackupScheduleConfiguration(
                HotBackupType,
                HotBackupEnabledKey,
                HotBackupCronScheduleKey,
                nameValue,
                backupTimeZone),
            GetBackupScheduleConfiguration(
                ColdBackupType,
                ColdBackupEnabledKey,
                ColdBackupCronScheduleKey,
                nameValue,
                backupTimeZone)
        };
    }

    private static Dictionary<string, string> GetBackupScheduleSettings()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddBackupScheduleSetting(result, LegacyCronScheduleKey, cron_schedule);
        AddBackupScheduleSetting(result, HotBackupEnabledKey, hot_backup_enabled);
        AddBackupScheduleSetting(result, HotBackupCronScheduleKey, hot_backup_cron_schedule);
        AddBackupScheduleSetting(result, ColdBackupEnabledKey, cold_backup_enabled);
        AddBackupScheduleSetting(result, ColdBackupCronScheduleKey, cold_backup_cron_schedule);
        AddBackupScheduleSetting(result, BackupCronTimeZoneKey, backup_cron_timezone);

        return result;
    }

    private static void AddBackupScheduleSetting(Dictionary<string, string> settings, string key, string? value)
    {
        if (value == null)
        {
            return;
        }

        settings[key] = value;
    }

    private static BackupScheduleConfiguration GetBackupScheduleConfiguration(
        string backupType,
        string enabledKey,
        string scheduleKey,
        Dictionary<string, string> nameValue,
        TimeZoneInfo? backupTimeZone)
    {
        bool isEnabled = GetBackupEnabledFlag(nameValue, enabledKey);
        if (!isEnabled)
        {
            return new BackupScheduleConfiguration(
                backupType: backupType,
                enabledKey: enabledKey,
                enabled: false,
                cronSchedule: string.Empty,
                scheduleKey: scheduleKey,
                useLegacyOneAmGate: false,
                backupTimeZone: backupTimeZone);
        }

        if (nameValue.TryGetValue(scheduleKey, out string? configuredSchedule))
        {
            if (string.IsNullOrWhiteSpace(configuredSchedule))
            {
                throw new InvalidOperationException($"Backup scheduling is enabled for '{backupType}', but '{scheduleKey}' is blank.");
            }

            string cronSchedule = configuredSchedule.Trim();
            ValidateCronSchedule(scheduleKey, cronSchedule);

            return new BackupScheduleConfiguration(
                backupType: backupType,
                enabledKey: enabledKey,
                enabled: true,
                cronSchedule: cronSchedule,
                scheduleKey: scheduleKey,
                useLegacyOneAmGate: false,
                backupTimeZone: backupTimeZone);
        }

        if (nameValue.TryGetValue(LegacyCronScheduleKey, out string? legacySchedule))
        {
            if (string.IsNullOrWhiteSpace(legacySchedule))
            {
                throw new InvalidOperationException($"Backup scheduling is enabled for '{backupType}', but fallback '{LegacyCronScheduleKey}' is blank.");
            }

            string cronSchedule = legacySchedule.Trim();
            ValidateCronSchedule(LegacyCronScheduleKey, cronSchedule);

            return new BackupScheduleConfiguration(
                backupType: backupType,
                enabledKey: enabledKey,
                enabled: true,
                cronSchedule: cronSchedule,
                scheduleKey: LegacyCronScheduleKey,
                useLegacyOneAmGate: true,
                backupTimeZone: backupTimeZone);
        }

        throw new InvalidOperationException(
            $"Backup scheduling is enabled for '{backupType}', but '{scheduleKey}' is missing and fallback '{LegacyCronScheduleKey}' is missing.");
    }

    private static bool GetBackupEnabledFlag(Dictionary<string, string> nameValue, string enabledKey)
    {
        if (!nameValue.TryGetValue(enabledKey, out string? rawValue))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new InvalidOperationException($"Backup enabled flag '{enabledKey}' is blank. Use true, false, 1, 0, yes, or no.");
        }

        switch (rawValue.Trim().ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
                return true;

            case "false":
            case "0":
            case "no":
                return false;

            default:
                throw new InvalidOperationException($"Backup enabled flag '{enabledKey}' has invalid value '{rawValue}'. Use true, false, 1, 0, yes, or no.");
        }
    }

    private static TimeZoneInfo? GetBackupTimeZone(Dictionary<string, string> nameValue)
    {
        if (!nameValue.TryGetValue(BackupCronTimeZoneKey, out string? timeZoneId) ||
            string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            return FindBackupTimeZone(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new InvalidOperationException($"Invalid {BackupCronTimeZoneKey} value '{timeZoneId}'. Use a valid IANA or Windows time zone id.", ex);
        }
        catch (InvalidTimeZoneException ex)
        {
            throw new InvalidOperationException($"Invalid {BackupCronTimeZoneKey} value '{timeZoneId}'. The time zone data could not be loaded.", ex);
        }
    }

    private static TimeZoneInfo FindBackupTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException) when (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsTimeZoneId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
        }
        catch (TimeZoneNotFoundException) when (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaTimeZoneId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
        }
    }

    private static void ValidateCronSchedule(string scheduleKey, string cronSchedule)
    {
        try
        {
            _ = new CronExpression(cronSchedule);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Invalid Quartz cron schedule for '{scheduleKey}': '{cronSchedule}'.", ex);
        }
    }

    private static void ScheduleBackupJob(Quartz.IScheduler scheduler, BackupScheduleConfiguration backupScheduleConfiguration)
    {
        IJobDetail job = JobBuilder.Create<mmria.services.vitalsimport.Backup_job>()
            .WithIdentity($"{backupScheduleConfiguration.BackupType}-backup-job", "backup")
            .UsingJobData(mmria.services.vitalsimport.Backup_job.BackupTypeJobDataKey, backupScheduleConfiguration.BackupType)
            .UsingJobData(mmria.services.vitalsimport.Backup_job.LegacyOneAmGateJobDataKey, backupScheduleConfiguration.UseLegacyOneAmGate ? "true" : "false")
            .UsingJobData(mmria.services.vitalsimport.Backup_job.TimeZoneIdJobDataKey, backupScheduleConfiguration.BackupTimeZone?.Id ?? string.Empty)
            .Build();

        var triggerBuilder = TriggerBuilder.Create()
            .WithIdentity($"{backupScheduleConfiguration.BackupType}-backup-trigger", "backup")
            .StartNow();

        triggerBuilder = backupScheduleConfiguration.BackupTimeZone == null
            ? triggerBuilder.WithCronSchedule(backupScheduleConfiguration.CronSchedule)
            : triggerBuilder.WithCronSchedule(
                backupScheduleConfiguration.CronSchedule,
                cronScheduleBuilder => cronScheduleBuilder.InTimeZone(backupScheduleConfiguration.BackupTimeZone));

        scheduler.ScheduleJob(job, triggerBuilder.Build()).GetAwaiter().GetResult();

        string timeZoneLabel = backupScheduleConfiguration.BackupTimeZone == null
            ? "default server time"
            : $"time zone '{backupScheduleConfiguration.BackupTimeZone.Id}'";
        string legacyLabel = backupScheduleConfiguration.UseLegacyOneAmGate
            ? " with legacy 1:00 AM backup window"
            : string.Empty;

        Console.WriteLine(
            $"[BackupSchedule] Scheduled {backupScheduleConfiguration.BackupType} automatic backup using {backupScheduleConfiguration.ScheduleKey} '{backupScheduleConfiguration.CronSchedule}'{legacyLabel} ({timeZoneLabel}).");
    }

    private sealed class BackupScheduleConfiguration
    {
        public BackupScheduleConfiguration(
            string backupType,
            string enabledKey,
            bool enabled,
            string cronSchedule,
            string scheduleKey,
            bool useLegacyOneAmGate,
            TimeZoneInfo? backupTimeZone)
        {
            BackupType = backupType;
            EnabledKey = enabledKey;
            Enabled = enabled;
            CronSchedule = cronSchedule;
            ScheduleKey = scheduleKey;
            UseLegacyOneAmGate = useLegacyOneAmGate;
            BackupTimeZone = backupTimeZone;
        }

        public string BackupType { get; }
        public string EnabledKey { get; }
        public bool Enabled { get; }
        public string CronSchedule { get; }
        public string ScheduleKey { get; }
        public bool UseLegacyOneAmGate { get; }
        public TimeZoneInfo? BackupTimeZone { get; }
    }
}

