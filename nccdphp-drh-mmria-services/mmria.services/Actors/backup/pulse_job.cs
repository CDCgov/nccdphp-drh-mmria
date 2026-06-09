using System;
using System.Threading.Tasks;
using Quartz;

namespace mmria.services.vitalsimport;

public sealed class Pulse_job : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        //System.Console.WriteLine($"Quartz_Pulse - {DateTime.Now:r}");

        var quartzSupervisor = Program.ActorSystem.ActorSelection("akka://mmria-actor-system/user/QuartzSupervisor");
        quartzSupervisor.Tell("pulse");

        return Task.CompletedTask;
    }
}

public sealed class Backup_job : IJob
{
    public const string BackupTypeJobDataKey = "backup_type";
    public const string LegacyOneAmGateJobDataKey = "legacy_one_am_gate";
    public const string TimeZoneIdJobDataKey = "backup_timezone_id";

    public Task Execute(IJobExecutionContext context)
    {
        string backupType = context.MergedJobDataMap.GetString(BackupTypeJobDataKey) ?? string.Empty;
        if (!IsSupportedBackupType(backupType))
        {
            throw new JobExecutionException($"Invalid scheduled backup type '{backupType}'. Expected 'hot' or 'cold'.");
        }

        string timeZoneId = context.MergedJobDataMap.GetString(TimeZoneIdJobDataKey);
        if (IsLegacyOneAmGateEnabled(context) && !IsLegacyBackupWindow(timeZoneId))
        {
            return Task.CompletedTask;
        }

        if (Program.ActorSystem == null)
        {
            throw new JobExecutionException("Unable to dispatch scheduled backup because the Akka actor system is not initialized.");
        }

        Console.WriteLine($"[BackupSchedule] Dispatching scheduled {backupType} backup at {DateTime.Now:r}.");

        var backupSupervisor = Program.ActorSystem.ActorSelection("akka://mmria-actor-system/user/backup-supervisor");
        backupSupervisor.Tell(new mmria.services.backup.BackupSupervisor.PerformBackupMessage
        {
            type = backupType,
            DateStarted = DateTime.Now,
            ReturnToSender = false
        });

        return Task.CompletedTask;
    }

    private static bool IsSupportedBackupType(string backupType)
    {
        return string.Equals(backupType, "hot", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(backupType, "cold", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLegacyOneAmGateEnabled(IJobExecutionContext context)
    {
        string value = context.MergedJobDataMap.GetString(LegacyOneAmGateJobDataKey);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLegacyBackupWindow(string timeZoneId)
    {
        DateTime currentTime = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            currentTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime;
        }

        return currentTime.Hour == 1 && currentTime.Minute == 0;
    }
}
