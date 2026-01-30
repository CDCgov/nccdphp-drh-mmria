using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.DI.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Akka.Quartz.Actor;
using Quartz;
using Quartz.Impl;


namespace mmria.services.vitalsimport;

public sealed class Program
{
    //public static Akka.Actor.ActorSystem actorSystem;

    public static string config_web_site_url = null;
    public static string  couchdb_url;
    public static string db_prefix;
    public static string timer_user_name;
    public static string timer_value;

    public static string central_couchdb_url = null;
    public static string central_timer_user_name = null;
    public static string central_timer_value = null;

    public static string vitals_service_key = null;
    public static string config_id;

    public static Akka.Actor.ActorSystem ActorSystem;

    public static mmria.common.couchdb.ConfigurationSet DbConfigSet;

    private static IConfiguration configuration;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add appsettings.local.json to configuration
        builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

        configuration = builder.Configuration;

        if (bool.Parse (configuration["mmria_settings:is_environment_based"])) 
        {
            Program.config_web_site_url = System.Environment.GetEnvironmentVariable ("web_site_url");
            //Program.config_export_directory = System.Environment.GetEnvironmentVariable ("export_directory") != null ? System.Environment.GetEnvironmentVariable ("export_directory") : "/workspace/export";
            Program.couchdb_url = System.Environment.GetEnvironmentVariable ("couchdb_url");
            Program.db_prefix = System.Environment.GetEnvironmentVariable ("db_prefix");
            Program.timer_user_name = System.Environment.GetEnvironmentVariable ("timer_user_name");
            Program.timer_value = System.Environment.GetEnvironmentVariable ("timer_password");
            Program.central_couchdb_url = System.Environment.GetEnvironmentVariable ("central_couchdb_url");
            Program.central_timer_user_name = System.Environment.GetEnvironmentVariable ("central_timer_password");
            Program.central_timer_value = System.Environment.GetEnvironmentVariable ("central_timer_password");
            Program.vitals_service_key = System.Environment.GetEnvironmentVariable ("vitals_service_key");
            Program.config_id = System.Environment.GetEnvironmentVariable ("config_id");

            configuration["mmria_settings:web_site_url"] = Program.config_web_site_url;
            //Program.config_export_directory = configuration["mmria_settings:export_directory"];
            configuration["mmria_settings:couchdb_url"] = Program.couchdb_url;
            configuration["mmria_settings:db_prefix"] = Program.db_prefix;
            configuration["mmria_settings:timer_user_name"] = Program.timer_user_name;
            configuration["mmria_settings:timer_value"] = Program.timer_value;
            configuration["mmria_settings:central_couchdb_url"] = Program.central_couchdb_url;
            configuration["mmria_settings:central_timer_password"] = Program.central_timer_user_name;
            configuration["mmria_settings:central_timer_password"] = Program.central_timer_value;
            configuration["mmria_settings:vitals_service_key"] = Program.vitals_service_key;
            configuration["mmria_settings:config_id"] = Program.config_id;
        }
        else 
        {
            Program.config_web_site_url = configuration["mmria_settings:web_site_url"];
            //Program.config_export_directory = configuration["mmria_settings:export_directory"];
            Program.couchdb_url = configuration["mmria_settings:couchdb_url"];
            Program.db_prefix = configuration["mmria_settings:db_prefix"];
            Program.timer_user_name = configuration["mmria_settings:timer_user_name"];
            Program.timer_value = configuration["mmria_settings:timer_password"];

            Program.central_couchdb_url = configuration["mmria_settings:central_couchdb_url"];
            Program.central_timer_user_name = configuration["mmria_settings:central_timer_password"];
            Program.central_timer_value = configuration["mmria_settings:central_timer_password"];
            Program.vitals_service_key = configuration["mmria_settings:vitals_service_key"];
            Program.config_id = configuration["mmria_settings:config_id"];
        }

        DbConfigSet = GetConfiguration();

        builder.Services.AddControllers();

        builder.Services.AddAuthentication("BasicAuthentication")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, mmria.services.Classes.HeaderAuthenticationHandler>("BasicAuthentication", null);

        builder.Services.AddSingleton<mmria.common.couchdb.ConfigurationSet>(DbConfigSet);


        var collection = new ServiceCollection();

        collection.AddSingleton<mmria.common.couchdb.ConfigurationSet>(DbConfigSet);
        collection.AddSingleton<IConfiguration>(configuration);
        collection.AddLogging();

        var provider = collection.BuildServiceProvider();

        var actorSystem = ActorSystem.Create("mmria-actor-system").UseServiceProvider(provider);
        actorSystem.ActorOf<RecordsProcessor_Worker.Actors.BatchSupervisor>("batch-supervisor");
        actorSystem.ActorOf<mmria.services.backup.BackupSupervisor>("backup-supervisor");
        actorSystem.ActorOf<mmria.services.populate_cdc_instance.PopulateCDCInstanceSupervisor>("populate-cdc-instance-supervisor");
        
        builder.Services.AddHostedService<Worker>();
        builder.Services.AddSingleton(typeof(ActorSystem), (serviceProvider) => actorSystem);


        Program.ActorSystem = actorSystem;

        var quartzSupervisor = actorSystem.ActorOf(Props.Create<mmria.server.model.actor.QuartzSupervisor>(), "QuartzSupervisor");

        quartzSupervisor.Tell("init");

        
        ISchedulerFactory schedFact = new StdSchedulerFactory();
        Quartz.IScheduler sched = schedFact.GetScheduler().Result;

        // compute a time that is on the next round minute
        DateTimeOffset runTime = DateBuilder.EvenMinuteDate(DateTimeOffset.UtcNow);

        // define the job and tie it to our HelloJob class
        IJobDetail job = JobBuilder.Create<mmria.services.vitalsimport.Pulse_job>()
            .WithIdentity("job1", "group1")
            .Build();

        // Trigger the job to run on the next round minute
        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity("trigger1", "group1")
            .StartAt(runTime.AddMinutes(3))
            .WithCronSchedule(DbConfigSet.name_value["cron_schedule"])
            .Build();

        sched.ScheduleJob(job, trigger);

        sched.Start();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();

        }
        else
        {
            app.UseHttpsRedirection();
            //app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            //app.UseHsts();
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        //app.MapRazorPages();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        app.Run(config_web_site_url);
    }


    private static mmria.common.couchdb.ConfigurationSet GetConfiguration()
    {
        var result = new mmria.common.couchdb.ConfigurationSet();
        try
        {
            string request_string = $"{mmria.services.vitalsimport.Program.couchdb_url}/configuration/{mmria.services.vitalsimport.Program.config_id}";
            var case_curl = new mmria.getset.cURL("GET", null, request_string, null, mmria.services.vitalsimport.Program.timer_user_name, mmria.services.vitalsimport.Program.timer_value);
            string responseFromServer = case_curl.execute();
            result = Newtonsoft.Json.JsonConvert.DeserializeObject<mmria.common.couchdb.ConfigurationSet> (responseFromServer);
            if
            (
                result!= null &&
                result.name_value.ContainsKey("metadata_version")
            )
            {
                Console.WriteLine($"metadata version: {result.name_value["metadata_version"]}");
            }

        }
        catch(Exception ex)
        {
            Console.WriteLine (ex);
        } 

        return result;
    }

    
}

