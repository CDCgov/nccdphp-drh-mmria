# MMRIA Services & Background Jobs Documentation

- Status: Active
- Scope: `mmria-server` and `mmria.services` background jobs, actors, Quartz schedules, and host responsibilities.
- When to use: Read this before changing scheduled work, actor wiring, or background processing responsibilities.
- Last verified: 2026-04-14
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Multi-Tenant Rebuild Process](./multi_tenant_rebuild_process.md), [Populate CDC Instance and De-identification Context](./populate_cdc_deidentification_context.md)

This doc is intentionally a routing and architecture summary, not a full copy of every actor implementation. Use it to identify ownership, schedules, and risk boundaries, then jump to the linked source files for message contracts and detailed control flow.

## mmria.services

### Overview
The mmria.services project is a standalone .NET service application that handles background processing tasks including vital records import, database backup operations, batch processing, and data synchronization with CDC instances. It runs independently of the main mmria-server web application and communicates via Akka.NET actor system and Quartz.NET scheduling.

### Architecture
- **Framework:** ASP.NET Core Web API
- **Actor System:** Akka.NET (mmria-actor-system)
- **Scheduling:** Quartz.NET
- **Database:** CouchDB
- **Port:** Configured via `web_site_url` (default: http://localhost:8080)

### Runtime summary

| Component | Role | Trigger / cadence | Primary files |
| --- | --- | --- | --- |
| `Worker` | Hosts the long-running Akka.NET runtime and supervisor references | Continuous background service on startup | [mmria.services/Worker.cs](../../nccdphp-drh-mmria-services/mmria.services/Worker.cs) |
| `Pulse_job` | Quartz heartbeat that drives scheduled backup evaluation | Cron schedule, default every minute | [mmria.services/Actors/backup/pulse_job.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/backup/pulse_job.cs) |
| `QuartzSupervisor` | Interprets pulse timing and decides when backup work should run | Receives `init` and `pulse` actor messages | [mmria.services/Actors/backup/QuartzSupervisor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/backup/QuartzSupervisor.cs) |
| `BatchSupervisor` | Coordinates IJE/vitals batch processing and child batch workers | Vitals import controller and batch status messages | [mmria.services/Actors/BatchSupervisor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/BatchSupervisor.cs) |
| `BackupSupervisor`, `BackupHotProcessor`, `BackupColdProcessor`, `FileCompressor` | Execute hot backup, cold backup, and archive-compression work | Backup pulse windows, nominally daily around 1:00 AM | [mmria.services/Actors/backup/BackupSupervisor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/backup/BackupSupervisor.cs) |
| `PopulateCDCInstanceSupervisor` and child populate actors | Push jurisdiction case data to CDC and track run status | Manual/API-triggered | [mmria.services/Actors/populate-cdc-instance/PopulateCDCInstanceSupervisor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/PopulateCDCInstanceSupervisor.cs) |
| `Recieve_Import_Actor` | Parses uploaded FET/MOR/NAT files and starts import processing | Upload/import messages | [mmria.services/Actors/VitalsImport/Recieve_Import_Actor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/VitalsImport/Recieve_Import_Actor.cs) |

### What matters when editing

- `mmria.services` is still the host for actor wiring, scheduling, and service startup, even when shared service logic moves into `mmria.common/SharedLibraries/MMRIAServices`.
- Startup now uses strict fail-fast configuration loading and a single service-provider shape. Use the refactor-risk doc for the current startup constraints before changing `Program.cs`.
- Backup and CDC-population behavior are contract-sensitive because they coordinate across actors, Quartz timing, and external CouchDB state.

### Startup flow

1. Load required configuration and `ConfigurationSet` data through the strict startup loader path in [mmria.services/Program.cs](../../nccdphp-drh-mmria-services/mmria.services/Program.cs).
2. Build the DI container and Akka.NET runtime once, then register supervisor actors from that same app provider.
3. Create the Quartz scheduler and schedule `Pulse_job` using `mmria_settings:cron_schedule`.
4. Start the Web API host and keep hosted background services running.

## mmria-server

### Overview
The mmria-server is the primary web application providing the user interface and API for the MMRIA (Maternal Mortality Review Information Application) system. It uses Akka.NET actors and Quartz.NET for background job processing, handles multi-tenant deployments, and coordinates data synchronization operations.

### Architecture
- **Framework:** ASP.NET Core Web Application
- **Actor System:** Akka.NET (mmria-actor-system)
- **Scheduling:** Quartz.NET
- **Database:** CouchDB (multiple instances in multi-tenant mode)
- **Port:** Configured via `web_site_url` (default: http://*:8080)
- **Multi-Tenant Support:** Yes (configurable via `multi_tenant_jurisdictions`)

### Runtime summary

| Component | Role | Trigger / cadence | Primary files |
| --- | --- | --- | --- |
| `Pulse_job` | Global heartbeat that fans out scheduled work to tenant Quartz supervisors | Cron schedule when `is_schedule_enabled=true` | [mmria-server/model/actor/quartz/Pulse_Job.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Pulse_Job.cs) |
| `QuartzSupervisor-{tenant}` | Per-tenant coordinator for midnight work, DB checks, and CDC pull orchestration | Receives `init` and recurring `pulse` messages | [mmria-server/model/actor/QuartzSupervisor.cs](../../source-code/mmria/mmria-server/model/actor/QuartzSupervisor.cs) |
| `Check_DB_Install` | Verifies core CouchDB/system-database setup | Spawned from `QuartzSupervisor` when DB checks are enabled | [mmria-server/model/actor/quartz/Check_DB_Install.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Check_DB_Install.cs) |
| `Process_Central_Pull_list` | Rebuilds pulled jurisdiction data from CDC into local tenant databases | Scheduled by `QuartzSupervisor`, conditional on `cdc_instance_pull_list` | [mmria-server/model/actor/quartz/Process_Central_Pull_list.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Process_Central_Pull_list.cs) |
| `Rebuild_Export_Queue` | Clears and recreates the export queue database and export directory | Midnight maintenance | [mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs) |
| `Process_DB_Synchronization_Set`, `Synchronize_Deleted_Case_Records`, `Synchronize_Case` | Keep `mmrds`, `de_id`, and report databases aligned as case data changes | Change-feed driven and save-triggered | [mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs) |
| `Process_Migrate_Data` | Applies metadata-driven migration plans when schema changes require data transforms | Manual or startup-triggered migration workflows | [mmria-server/model/actor/quartz/Process_Migrate_Data.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Process_Migrate_Data.cs) |
| `SteveAPISupervisor` | Coordinates STEVE integration requests | API-triggered or scheduled integration work | [mmria-server/model/actor/SteveAPISupervisor.cs](../../source-code/mmria/mmria-server/model/actor/SteveAPISupervisor.cs) |
| `Post_Session_Actor`, `Record_Session_Event`, file-writer actors | Session persistence, audit logging, and file-write side effects | Request-driven | [mmria-server/model/actor/Post_Session_Actor.cs](../../source-code/mmria/mmria-server/model/actor/Post_Session_Actor.cs) |

### What matters when editing

- Multi-tenant scheduling is still server-owned. New request-path refactors should not move actor creation or host orchestration into `mmria.common` unless the task explicitly broadens scope.
- `QuartzSupervisor` is the key fan-out point for per-tenant work. Changes there affect DB install checks, export rebuild cadence, CDC pull behavior, and other recurring maintenance.
- The data-sync actors are contract-sensitive because they keep case, de-identified, and report databases consistent after saves, deletes, and migrations.

### Startup flow

1. Load tenant/shared configuration and runtime services in [mmria-server/Program.cs](../../source-code/mmria/mmria-server/Program.cs).
2. Resolve multi-tenant startup settings, rebuild-summary context, and per-tenant configuration state.
3. Register DI services, create the Akka.NET runtime, and spawn per-tenant `QuartzSupervisor` actors plus shared supervisors such as STEVE integration.
4. Create and start Quartz with `Pulse_job` when scheduling is enabled.
5. Start authentication, authorization, logging, and MVC/API hosting.

---

## Configuration Reference

### mmria.services Configuration

| Setting | Purpose | Default | Location |
|---------|---------|---------|----------|
| `is_environment_based` | Use environment variables instead of appsettings | `true` | appsettings.json |
| `web_site_url` | Service listening URL | `http://localhost:8080` | appsettings.json |
| `couchdb_url` | CouchDB server URL | `http://cdc-couchdb.local:5984/` | appsettings.json |
| `db_prefix` | Database name prefix | `""` (empty) | appsettings.json |
| `timer_user_name` | CouchDB admin username | (required) | appsettings.json |
| `timer_password` | CouchDB admin password | (required) | appsettings.json |
| `config_id` | Configuration document ID | `mmria-services` | appsettings.json |
| `vitals_import_additional_tenants` | Comma-separated exact tenant suffixes allowed for vital import MOR filenames beyond states and territories | `""` | appsettings.json |
| `cron_schedule` | Quartz job schedule | `0 */1 * * * ?` | appsettings.json |
| `central_couchdb_url` | CDC central CouchDB URL | `http://cdc-couchdb.local:5984` | appsettings.json |
| `vitals_service_key` | Authentication key for vitals API | `null` | appsettings.json |
| `metadata_version` | Current metadata schema version | `25.08.14` | appsettings.json |
| `log_directory` | Log file output directory | `c:/temp/mmrds/mmria-log` | appsettings.json |
| `export_directory` | Export file storage directory | `c:/temp/mmrds/mmria-export` | appsettings.json |
| `startup_rebuild_max_concurrent_tenants` | Max concurrent tenant rebuild executions | `1` | appsettings.json |
| `startup_rebuild_page_size` | Startup rebuild page size | `100` | appsettings.json |
| `startup_rebuild_batch_delay_ms` | Delay between rebuild batches | `250` | appsettings.json |
| `startup_rebuild_bulk_write_retry_count` | Bulk write retry count | `3` | appsettings.json |
| `startup_rebuild_bulk_write_retry_delay_ms` | Bulk write retry delay in ms | `1500` | appsettings.json |
| `startup_rebuild_progress_persist_every_batches` | Summary persistence cadence | `10` | appsettings.json |

**Cron Schedule Format:** `0 */1 * * * ?` = every minute at second 0
- Format: `second minute hour day-of-month month day-of-week`

---

### mmria-server Configuration

| Setting | Purpose | Default | Location |
|---------|---------|---------|----------|
| `is_environment_based` | Use environment variables instead of appsettings | `true` | appsettings.json |
| `web_site_url` | Server listening URL | `http://*:8080` | appsettings.json |
| `couchdb_url` | Primary CouchDB URL | `http://localhost:5984` | appsettings.json |
| `db_prefix` | Database name prefix | `""` (empty) | appsettings.json |
| `timer_user_name` | CouchDB admin username | (required) | appsettings.json |
| `timer_value` / `timer_password` | CouchDB admin password | (required) | appsettings.json |
| `config_id` | Configuration document ID | (tenant-specific) | appsettings.json |
| `shared_config_id` | Shared configuration ID | (required) | appsettings.json |
| `cron_schedule` | Quartz Pulse_job schedule | `0 */1 * * * ?` | appsettings.json |
| `is_schedule_enabled` | Enable Quartz scheduler | `true` | appsettings.json |
| `is_db_check_enabled` | Enable DB install checks | `true` | appsettings.json |
| `metadata_version` | Current metadata schema version | `20.12.01` | appsettings.json |
| `log_directory` | Log file output directory | `c:/temp/mmrds/mmria-log` | appsettings.json |
| `export_directory` | Export file storage directory | `c:/temp/mmrds/mmria-export` | appsettings.json |
| `multi_tenant_jurisdictions` | Comma-separated tenant list | `""` (empty = single tenant) | appsettings.json |
| `multi_tenant_db_rebuild` | Enable startup rebuild queueing | `true` | appsettings.json |
| `multi_tenant_jurisdictions_rebuild` | Startup rebuild tenant list; falls back to `multi_tenant_jurisdictions` when empty | `""` | appsettings.json |
| `multi_tenant_re_build_src` | Optional startup summary host override | `""` | appsettings.json |
| `multi_tenant_shared_config_id` | Config ID for multi-tenant | (required if multi-tenant) | appsettings.json |
| `multi_tenant_shared_config_id_template_couchdb_url` | CouchDB URL template with {replace} placeholder | (required if multi-tenant) | appsettings.json |
| `app_instance_name` | Instance identifier | `""` | appsettings.json |
| `session_idle_timeout_minutes` | Session timeout | `70` | appsettings.json |
| `sams:is_enabled` | Enable SAMS authentication | `false` | appsettings.json |
| `vitals_url` | Vitals import service URL | `http://localhost:44331/api/Message/IJESet` | appsettings.json |
| `vitals_import_additional_tenants` | Comma-separated exact tenant suffixes allowed for client-side vital upload filename validation beyond states and territories | `""` | appsettings.json |
| `geocode_api_url` | Geocoding service URL | (optional) | appsettings.json |
| `geocode_api_key` | Geocoding API key | (optional) | appsettings.json |

**Multi-Tenant Example:**
```json
{
  "multi_tenant_jurisdictions": "NC,GA,FL",
  "multi_tenant_shared_config_id": "shared_config",
  "multi_tenant_shared_config_id_template_couchdb_url": "http://couchdb-{replace}.local:5984"
}
```
This creates QuartzSupervisors for NC, GA, and FL with URLs:
- `http://couchdb-NC.local:5984`
- `http://couchdb-GA.local:5984`
- `http://couchdb-FL.local:5984`

Startup rebuild ownership note:
- `mmria-server` owns queue/context settings such as `multi_tenant_db_rebuild`, `multi_tenant_jurisdictions_rebuild`, and `multi_tenant_re_build_src`.
- `mmria.services` owns execution tuning such as `startup_rebuild_page_size` and `startup_rebuild_max_concurrent_tenants`.

---

## Job Execution Schedule Summary

### mmria.services

| Job/Actor | Frequency | Time | Trigger |
|-----------|-----------|------|---------|
| Pulse_job | Every minute | * | Quartz cron |
| BackupSupervisor | Daily | 1:00 AM | Pulse message |
| Hot Backup | Daily | 1:00 AM | BackupSupervisor |
| Cold Backup | Daily | 1:00 AM | BackupSupervisor |
| BatchSupervisor | On demand | * | API call |
| PopulateCDCInstanceSupervisor | On demand | * | API call |

### mmria-server

| Job/Actor | Frequency | Time | Trigger | Enabled By |
|-----------|-----------|------|---------|------------|
| Pulse_job | Every minute | * | Quartz cron | `is_schedule_enabled` |
| QuartzSupervisor (per tenant) | Every minute | * | Pulse_job | `is_schedule_enabled` |
| Check_DB_Install | Every minute | * | QuartzSupervisor | `is_db_check_enabled` |
| Process_Central_Pull_list | Daily | 12:00 AM | QuartzSupervisor | `is_schedule_enabled` |
| Rebuild_Export_Queue | Daily | 12:00 AM | QuartzSupervisor | `is_schedule_enabled` |
| Process_DB_Synchronization_Set | On change | * | Case modifications | Always active |
| Synchronize_Case | On change | * | Case save/delete | Always active |
| SteveAPISupervisor | On demand | * | API call | Always active |

**Legend:**
- `*` = No specific time, runs continuously or on trigger
- On demand = Triggered by user action or API call
- On change = Triggered by database change detection

---

## Key Design Patterns

### Actor Model
Both systems use Akka.NET's actor model for concurrent, asynchronous processing:
- **Supervisors** manage child actors and coordinate work
- **Message passing** enables decoupled communication
- **Self-terminating actors** clean up after completing tasks
- **Actor selection** patterns allow dynamic actor lookup

### Scheduling Strategy
- **Quartz.NET** provides reliable cron-based scheduling
- **Pulse pattern** sends periodic heartbeat to trigger checks
- **Time-based logic** in actors determines if work should execute
- **Once-daily operations** use time checks (e.g., `difference.Hour == 0`)

### Multi-Tenancy
- **Per-tenant actors** allow isolated processing for each jurisdiction
- **Configuration isolation** separates database and settings per tenant
- **Dynamic actor creation** spawns supervisors based on tenant list
- **Template URLs** enable flexible database endpoint configuration

### Database Synchronization
- **Change feed monitoring** detects record modifications
- **Incremental sync** processes only changed records
- **Sequence tracking** prevents duplicate processing
- **Multi-database sync** maintains consistency across mmrds, de_id, report databases

---

## Troubleshooting

### Jobs Not Running

1. **Check schedule enabled:** `is_schedule_enabled=true` in mmria-server
2. **Verify cron expression:** Default `0 */1 * * * ?` runs every minute
3. **Check logs:** Look in `log_directory` for errors
4. **Actor system:** Ensure actors are spawned in Program.cs

### Backups Not Running

1. **Check time:** Backups run at 1:00 AM (mmria.services) or 12:00 AM (mmria-server)
2. **Verify pulse:** Pulse_job should execute every minute
3. **Check QuartzSupervisor:** Should receive pulse messages
4. **Logs:** Look for "Quartz_Pulse" or backup messages

### Synchronization Issues

1. **Check change sequence:** `Program.Last_Change_Sequence` tracks progress
2. **Database connectivity:** Verify CouchDB URLs and credentials
3. **Permissions:** Ensure timer_user_name has admin rights
4. **Metadata version:** Check `metadata_version` matches across systems

### Multi-Tenant Issues

1. **Verify jurisdiction list:** Check `multi_tenant_jurisdictions` format
2. **CouchDB URLs:** Ensure {replace} template is correct
3. **Configuration docs:** Each tenant needs config in CouchDB
4. **Skip CDC tenants:** CDC and CDCQA are excluded from QuartzSupervisor

---

## Additional Resources

- **Akka.NET Documentation:** https://getakka.net/
- **Quartz.NET Documentation:** https://www.quartz-scheduler.net/
- **CouchDB Documentation:** https://docs.couchdb.org/
- **MMRIA Metadata:** See [docs/development_context.md](development_context.md)

---

**Document Version:** 1.0  
**Last Updated:** February 5, 2026
