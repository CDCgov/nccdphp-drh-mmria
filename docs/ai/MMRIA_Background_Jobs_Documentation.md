# MMRIA Services & Background Jobs Documentation

- Status: Active
- Scope: `mmria-server` and `mmria.services` background jobs, actors, Quartz schedules, and host responsibilities.
- When to use: Read this before changing scheduled work, actor wiring, or background processing responsibilities.
- Last verified: 2026-03-24
- Related docs: [AI Context Index](./AI_CONTEXT.md), [Multi-Tenant Rebuild Process](./multi_tenant_rebuild_process.md), [Populate CDC Instance and De-identification Context](./populate_cdc_deidentification_context.md)
**Generated:** February 5, 2026  
**Systems:** mmria-server and mmria.services

---

## Table of Contents
1. [mmria.services](#mmriaservices)
2. [mmria-server](#mmria-server)
3. [Configuration Reference](#configuration-reference)

---

## mmria.services

### Overview
The mmria.services project is a standalone .NET service application that handles background processing tasks including vital records import, database backup operations, batch processing, and data synchronization with CDC instances. It runs independently of the main mmria-server web application and communicates via Akka.NET actor system and Quartz.NET scheduling.

### Architecture
- **Framework:** ASP.NET Core Web API
- **Actor System:** Akka.NET (mmria-actor-system)
- **Scheduling:** Quartz.NET
- **Database:** CouchDB
- **Port:** Configured via `web_site_url` (default: http://localhost:8080)

---

### Hosted Services

#### 1. **Worker (BackgroundService)**
- **Purpose:** Core background service that hosts the Akka actor system for processing tasks
- **Type:** `BackgroundService` implementation
- **Schedule:** Runs continuously on startup
- **Key Operations:**
  - Initializes Akka.NET actor system
  - Maintains reference to vitals_import_queue
  - Hosts actor supervisors for batch processing, backup, and CDC population
- **Dependencies:** 
  - `ActorSystem`
  - `ILogger<Worker>`
- **Config Keys:** None (always active)
- **File:** [mmria.services/Worker.cs](../../nccdphp-drh-mmria-services/mmria.services/Worker.cs)

---

### Quartz Scheduled Jobs

#### 1. **Vitals Import Pulse Job**
- **Job Class:** `mmria.services.vitalsimport.Pulse_job`
- **Schedule:** Configured via `cron_schedule` (default: `0 */1 * * * ?` - every minute)
- **Purpose:** Sends periodic pulse messages to QuartzSupervisor to trigger backup operations
- **Trigger:** Automatic via cron expression, starts 3 minutes after application launch
- **Key Operations:**
  - Sends "pulse" message to QuartzSupervisor actor
  - Triggers scheduled backup operations at configured intervals
- **Config Keys:**
  - `mmria_settings:cron_schedule`
  - `mmria_settings:config_id`
- **File:** [mmria.services/Actors/backup/pulse_job.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/backup/pulse_job.cs)

---

### Actor System Jobs

#### 1. **BatchSupervisor**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Manages vitals import batch processing from IJE (Inter-Jurisdictional Exchange) files
- **Messages Handled:**
  - `NewIJESet_Message` - Processes new IJE record batches
  - `BatchStatusMessage` - Updates batch processing status
  - `BatchRemoveDataMessage` - Removes finished/rejected batch data
- **Key Operations:**
  - Monitors batch processing status (InProcess, Finished, BatchRejected)
  - Pings CVS (Certificate Verification Service) server before processing
  - Spawns child `BatchProcessor` actors for each batch
  - Maintains batch_id_list dictionary with processing states
- **Trigger:** Receives messages from vitals import controller
- **Dependencies:**
  - CVS Server API (pings before processing)
  - `CouchDbHttpClient`
- **File:** [mmria.services/Actors/BatchSupervisor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/BatchSupervisor.cs)

#### 2. **BackupSupervisor**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Coordinates database backup operations (hot, cold, and compression)
- **Messages Handled:**
  - `PerformBackupMessage` - Initiates backup operations
  - `BackupFinishedMessage` - Marks backup completion
- **Key Operations:**
  - **Hot Backup:** Online backup of active database
  - **Cold Backup:** Offline/snapshot backup
  - **Compress:** File compression of backup archives
- **Trigger:** 
  - Receives pulse message every minute from Pulse_job
  - Performs backups at 1:00 AM daily
- **Dependencies:** 
  - `BackupHotProcessor` actor
  - `BackupColdProcessor` actor
  - `FileCompressor` actor
  - `CouchDbHttpClient`
- **Config Keys:** Schedule determined by QuartzSupervisor pulse timing
- **File:** [mmria.services/Actors/backup/BackupSupervisor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/backup/BackupSupervisor.cs)

#### 3. **BackupHotProcessor**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Performs online ("hot") database backups while system is running
- **Trigger:** Receives `PerformBackupMessage` from BackupSupervisor
- **Key Operations:**
  - Backs up active CouchDB databases
  - Replicates database content to backup location
- **File:** [mmria.services/Actors/backup/BackupHotProcessor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/backup/BackupHotProcessor.cs)

#### 4. **PopulateCDCInstanceSupervisor**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Manages synchronization of case data to CDC (Central) instance
- **Messages Handled:**
  - `Populate_CDC_Instance` - Initiates data transfer to CDC
  - `PopulateFinished` - Records completion status
  - `DateTime` - Status check requests
  - `Status` - Error or completion messages
- **Key Operations:**
  - Transfers case records to CDC central database
  - Tracks transfer progress and duration
  - Maintains transfer state (Ready=0, InProgress=1, Error=2)
  - Provides status reports with date/time and duration
- **Trigger:** Manual via API call from mmria-server
- **Dependencies:** 
  - `PopulateCDCInstance` actor (child)
  - `CouchDbHttpClient`
- **State Tracking:**
  - `transfer_status_number` (0=Ready, 1=InProgress, 2=Error)
  - `date_submitted`, `date_completed`
  - `duration_in_hours`, `duration_in_minutes`
- **File:** [mmria.services/Actors/populate-cdc-instance/PopulateCDCInstanceSupervisor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/populate-cdc-instance/PopulateCDCInstanceSupervisor.cs)

#### 5. **Recieve_Import_Actor**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Processes vital records import files (FET, MOR, NAT formats)
- **Messages Handled:**
  - `RecordUpload_Message` - Processes uploaded files
  - `NewIJESet_Message` - Handles new IJE record sets
- **Key Operations:**
  - Extracts and converts FET (Fetal Death), MOR (Mortality), NAT (Natality) files
  - Validates record lengths
  - Processes fixed-width format files
- **Trigger:** Receives messages from file upload operations
- **File:** [mmria.services/Actors/VitalsImport/Recieve_Import_Actor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/VitalsImport/Recieve_Import_Actor.cs)

#### 6. **QuartzSupervisor (Services)**
- **Actor Type:** `UntypedActor`
- **Purpose:** Coordinates backup scheduling in mmria.services
- **Messages Handled:**
  - `"init"` - Initialization
  - `"pulse"` - Triggered every minute by Pulse_job
- **Key Operations:**
  - Checks if current time is 1:00 AM
  - Triggers hot and cold backup operations daily at 1:00 AM
  - Sends messages to BackupSupervisor
- **Schedule:** Evaluated every minute, executes at 1:00 AM
- **File:** [mmria.services/Actors/backup/QuartzSupervisor.cs](../../nccdphp-drh-mmria-services/mmria.services/Actors/backup/QuartzSupervisor.cs)

---

### Startup Tasks

Performed during `Program.cs` initialization:

1. **Configuration Loading**
   - Loads from environment variables or appsettings.json based on `is_environment_based`
   - Reads CouchDB configuration from configuration database
   - Gets `ConfigurationSet` with metadata version and cron schedule

2. **Actor System Initialization**
   - Creates Akka.NET actor system named "mmria-actor-system"
   - Registers dependency injection container with actors
   - Spawns supervisor actors:
     - `batch-supervisor` (BatchSupervisor)
     - `backup-supervisor` (BackupSupervisor)
     - `populate-cdc-instance-supervisor` (PopulateCDCInstanceSupervisor)

3. **Quartz Scheduler Setup**
   - Creates Quartz scheduler instance
   - Schedules Pulse_job with cron expression
   - Starts scheduler immediately

4. **Web API Setup**
   - Configures authentication (BasicAuthentication via HeaderAuthenticationHandler)
   - Registers controllers and endpoints
   - Starts listening on configured URL

**Configuration File:** [mmria.services/Program.cs](../../nccdphp-drh-mmria-services/mmria.services/Program.cs)

---

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

---

### Quartz Scheduled Jobs

#### 1. **Pulse_job**
- **Job Class:** `mmria.server.model.Pulse_job`
- **Schedule:** Configured via `cron_schedule` (default: `0 */1 * * * ?` - every minute)
- **Purpose:** Heartbeat job that triggers QuartzSupervisor actors to perform periodic tasks
- **Trigger:** Automatic via cron, starts 3 minutes after launch
- **Enabled:** Controlled by `is_schedule_enabled` setting
- **Key Operations:**
  - Sends "pulse" message to all tenant QuartzSupervisor actors
  - Uses actor selection pattern: `akka://mmria-actor-system/user/QuartzSupervisor-*`
- **Config Keys:**
  - `mmria_settings:cron_schedule`
  - `mmria_settings:is_schedule_enabled`
- **File:** [mmria-server/model/actor/quartz/Pulse_Job.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Pulse_Job.cs)

---

### Actor System Jobs

#### 1. **QuartzSupervisor (per tenant)**
- **Actor Type:** `UntypedActor`
- **Purpose:** Main coordinator for scheduled background operations for each tenant/jurisdiction
- **Instance Count:** One per tenant in multi-tenant deployments
- **Messages Handled:**
  - `"init"` - Initialization message
  - `"pulse"` - Triggered every minute by Pulse_job
- **Key Operations:**
  - **Database Install Check:** Spawns `Check_DB_Install` actor (if `is_db_check_enabled=true`)
  - **Midnight Tasks:** At 00:00, spawns `Rebuild_Export_Queue` actor
  - **Regular Tasks:** Spawns `Process_Central_Pull_list` actor for CDC data synchronization
- **Trigger:** Receives pulse every minute from Pulse_job
- **Dependencies:**
  - `OverridableConfiguration` (tenant-specific config)
  - `ConfigurationSet` (metadata and settings)
  - `CouchDbHttpClient`
- **Actor Name Pattern:** `QuartzSupervisor-{tenant}` (e.g., QuartzSupervisor-NC, QuartzSupervisor-GA)
- **File:** [mmria-server/model/actor/QuartzSupervisor.cs](../../source-code/mmria/mmria-server/model/actor/QuartzSupervisor.cs)

#### 2. **Check_DB_Install**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Ensures CouchDB is properly configured on startup/pulse
- **Trigger:** Created by QuartzSupervisor on each pulse (if enabled)
- **Key Operations:**
  - Checks if CouchDB admin user exists
  - Creates admin user if not present
  - Configures CouchDB settings:
    - CORS (Cross-Origin Resource Sharing)
    - Persistent cookies
    - Bind address and port
  - Creates system databases (_users, _replicator, _global_changes)
- **Lifecycle:** Self-terminating after completion
- **Config Keys:** `is_db_check_enabled`
- **File:** [mmria-server/model/actor/quartz/Check_DB_Install.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Check_DB_Install.cs)

#### 3. **Process_Central_Pull_list**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Synchronizes data from CDC central instance to jurisdictional instances
- **Schedule:** Runs once daily at midnight (00:00)
- **Trigger:** Created by QuartzSupervisor on non-midnight pulses
- **Execution Condition:** **Only runs if `cdc_instance_pull_list` configuration is not null**
- **Key Operations:**
  - Pulls list of cases from CDC central database
  - Recreates local mmrds database (DELETE then CREATE)
  - Sets up database security (roles: abstractor, data_analyst, timer)
  - Installs database design documents (sortable views, auth views)
  - Recreates de_id (de-identified) database
  - Recreates report database
- **Skip Logic:** Runs once daily; skips subsequent pulses until next midnight
- **Config Keys:** `cdc_instance_pull_list` (required - job will not run if null)
- **File:** [mmria-server/model/actor/quartz/Process_Central_Pull_list.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Process_Central_Pull_list.cs)

#### 4. **Rebuild_Export_Queue**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Rebuilds the export queue database and clears export directory
- **Schedule:** Runs once daily at midnight (00:00)
- **Trigger:** Created by QuartzSupervisor when current time is 00:00
- **Key Operations:**
  - Deletes all files in export_directory
  - Deletes export_queue database
  - Recreates export_queue database
  - Sets database security (admins and members: abstractor role)
- **Lifecycle:** Self-terminating after completion
- **Config Keys:** `export_directory`
- **File:** [mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Rebuild_Export_Queue.cs)

#### 5. **Process_DB_Synchronization_Set**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Synchronizes changes from mmrds database to de_id and report databases
- **Trigger:** Created by other actors when case data changes
- **Key Operations:**
  - Monitors CouchDB _changes feed
  - Detects new, modified, and deleted case records
  - Synchronizes changes to de-identified (de_id) and report databases
  - Handles both PUT (create/update) and DELETE operations
  - Removes orphaned records in de_id/report databases
- **Change Detection:** Uses `Program.Last_Change_Sequence` to track changes
- **File:** [mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Process_DB_Synchronization_Set.cs)

#### 6. **Synchronize_Deleted_Case_Records**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Specifically handles synchronization of deleted case records
- **Trigger:** Created when case deletions are detected
- **Key Operations:**
  - Monitors _changes feed for deleted records
  - Synchronizes deletions to de_id and report databases
  - Updates change sequence tracking
- **File:** [mmria-server/model/actor/quartz/Synchronize_Deleted_Case_Records.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Synchronize_Deleted_Case_Records.cs)

#### 7. **Process_Migrate_Data**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Handles data migration when metadata schema changes
- **Messages Handled:**
  - `string` (migration_plan_id)
  - `Process_Initial_Migrations_Message`
- **Key Operations:**
  - Loads migration plans from database-scripts/migration-plan-set
  - Applies field mappings and value transformations
  - Updates case records to new schema version
  - Triggers synchronization after migration
- **Trigger:** Manual or on startup for initial migrations
- **File:** [mmria-server/model/actor/quartz/Process_Migrate_Data.cs](../../source-code/mmria/mmria-server/model/actor/quartz/Process_Migrate_Data.cs)

#### 8. **Synchronize_Case**
- **Actor Type:** `UntypedActor`
- **Purpose:** Synchronizes individual case records across databases
- **Messages Handled:**
  - `Sync_Document_Message` - Sync single document
  - `Sync_All_Documents_Message` - Sync all documents
- **Key Operations:**
  - Creates/updates/deletes records in synchronized databases
  - Applies metadata version transformations
  - Handles both single document and bulk synchronization
- **Dependencies:**
  - `c_sync_document` utility class
  - `c_document_sync_all` utility class
- **File:** [mmria-server/model/actor/Synchronize_Case.cs](../../source-code/mmria/mmria-server/model/actor/Synchronize_Case.cs)

#### 9. **SteveAPISupervisor**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Coordinates external API integration with STEVE (State Tracking of Electronic Vital Events) system
- **Messages Handled:**
  - `DownloadRequest` - Downloads data from STEVE API
- **Key Operations:**
  - Spawns `SteveAPI_Instance` actors for each request
  - Manages API authentication and requests
- **Trigger:** Receives messages from controllers or scheduled operations
- **File:** [mmria-server/model/actor/SteveAPISupervisor.cs](../../source-code/mmria/mmria-server/model/actor/SteveAPISupervisor.cs)

#### 10. **Post_Session Actor**
- **Actor Type:** `ReceiveActor`
- **Purpose:** Handles user session creation and management
- **Messages Handled:**
  - `Session_Message` - Creates/updates session records
- **Key Operations:**
  - Persists session data to database
  - Tracks session creation, updates, and expiration
  - Manages user authentication state
- **File:** [mmria-server/model/actor/Post_Session_Actor.cs](../../source-code/mmria/mmria-server/model/actor/Post_Session_Actor.cs)

#### 11. **Record_Session_Event**
- **Actor Type:** `UntypedActor`
- **Purpose:** Logs session-related events for auditing
- **Key Operations:**
  - Records login/logout events
  - Tracks user activity
  - Maintains audit trail
- **File:** [mmria-server/model/actor/Record_Session_Event.cs](../../source-code/mmria/mmria-server/model/actor/Record_Session_Event.cs)

#### 12. **FileDataWriterSupervisor & FileDataWriter**
- **Actor Type:** `UntypedActor`
- **Purpose:** Handles file upload and storage operations
- **Key Operations:**
  - Manages file writes to disk
  - Supervises child FileDataWriter actors
  - Handles file data persistence
- **Source note:** No matching `FileDataSupervisor.cs` file is present in the current workspace; verify this actor description before treating it as current implementation.

---

### Background Operations

#### Change Tracking System
- **Purpose:** Monitors CouchDB _changes feed to detect record modifications
- **Mechanism:** Uses `Program.Last_Change_Sequence` to track last processed change
- **Frequency:** Checked on every pulse (every minute)
- **Operations:**
  - Increments `Program.Change_Sequence_Call_Count`
  - Records timestamps in `Program.DateOfLastChange_Sequence_Call`
  - Limits timestamp list to 10 most recent entries

---

### Startup Tasks

Performed during `Program.cs` initialization:

1. **Configuration Loading**
   - Determines configuration source (environment variables vs appsettings.json)
   - Loads database configurations for all tenants
   - Gets `ConfigurationSet` and `OverridableConfiguration` from CouchDB

2. **Multi-Tenant Setup**
   - Parses `multi_tenant_jurisdictions` comma-separated list
   - Loads separate configuration for each tenant
   - Creates tenant-specific CouchDB URL patterns

3. **Dependency Injection**
   - Registers `ConfigurationSet` and `OverridableConfiguration`
   - Registers `CouchDbHttpClient` as singleton
   - Creates separate service collection for actors

4. **Actor System Initialization**
   - Creates Akka.NET actor system "mmria-actor-system"
   - Configures cluster settings (if clustering enabled)
   - Spawns QuartzSupervisor for each tenant (except CDC tenants)
   - Creates `steve-api-supervisor` actor

5. **Quartz Scheduler Setup**
   - Creates scheduler with Pulse_job
   - Configures cron trigger from settings
   - Starts scheduler if `is_schedule_enabled=true`

6. **Authentication Setup**
   - Configures SAMS (if `sams:is_enabled=true`)
   - Otherwise uses custom authentication
   - Sets up authorization policies (abstractor, data_analyst, etc.)

7. **Logging Configuration**
   - Sets up Serilog with file and console outputs
   - Configures log rotation (daily)
   - Logs to `log_directory` path

**Configuration File:** [mmria-server/Program.cs](../../source-code/mmria/mmria-server/Program.cs)

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
| `cron_schedule` | Quartz job schedule | `0 */1 * * * ?` | appsettings.json |
| `central_couchdb_url` | CDC central CouchDB URL | `http://cdc-couchdb.local:5984` | appsettings.json |
| `vitals_service_key` | Authentication key for vitals API | `null` | appsettings.json |
| `metadata_version` | Current metadata schema version | `25.08.14` | appsettings.json |
| `log_directory` | Log file output directory | `c:/temp/mmrds/mmria-log` | appsettings.json |
| `export_directory` | Export file storage directory | `c:/temp/mmrds/mmria-export` | appsettings.json |

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
| `multi_tenant_shared_config_id` | Config ID for multi-tenant | (required if multi-tenant) | appsettings.json |
| `multi_tenant_shared_config_id_template_couchdb_url` | CouchDB URL template with {replace} placeholder | (required if multi-tenant) | appsettings.json |
| `app_instance_name` | Instance identifier | `""` | appsettings.json |
| `session_idle_timeout_minutes` | Session timeout | `70` | appsettings.json |
| `sams:is_enabled` | Enable SAMS authentication | `false` | appsettings.json |
| `vitals_url` | Vitals import service URL | `http://localhost:44331/api/Message/IJESet` | appsettings.json |
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





