# Story 12.1 — Data Migration: Environment Configuration Parity with Replication Project

**Epic:** 12 — Data Migration Tool Modernization
**Story ID:** 12.1
**Status:** not-started
**Date added:** 2026-07-07

---

## User Story

As a developer running a data migration,
When I need to target a specific environment (QA, Integration, Production, Localhost),
I want to set the environment in `appsettings.local.json` rather than editing source code,
So that I can switch environments and credentials without touching `Program.cs` or committing secrets.

---

## Acceptance Criteria

**AC-1 — `appsettings.local.json` controls environment targeting**
Given `appsettings.local.json` has `EnvironmentSettings.ConfigEnvironment = "QA"`
When the migration runs
Then it connects to the CouchDB URL constructed from `CouchDBSettings.DatabaseUrlTemplates.QA`
And uses credentials from `Credentials["QA"]`
And iterates the prefix list from `JurisdictionLists.QA`

**AC-2 — `appsettings.json` committed with blank/safe defaults**
Given the developer clones the repo
When they open `data-migration/appsettings.json`
Then all `Username` and `Password` fields are empty strings
And `ConfigEnvironment` defaults to `"QA"`
And URL templates contain the same environment URL templates as the Replication project's `appsettings.json`

**AC-3 — `appsettings.local.json` is gitignored**
Given the developer creates `data-migration/appsettings.local.json` with real credentials
When they run `git status`
Then `appsettings.local.json` does not appear as a tracked or staged file

**AC-4 — `Configuration.cs` typed model mirrors the Replication project**
Given the new `Configuration.cs` file in `data-migration/`
When the developer reads it
Then it contains `DataMigrationAppConfiguration` with sections: `MigrationSettings`, `EnvironmentSettings`, `CouchDBSettings` (with `DatabaseUrlTemplates`), `Credentials` (dictionary by environment name), and `JurisdictionLists` (dictionary by environment name)
And `CredentialConfig` class has `Username` and `Password` string properties

**AC-5 — Hardcoded `run_list`, `test_list`, `prefix_list` removed from `Program.cs`**
Given the refactored `Program.cs`
When it executes
Then the active jurisdiction prefix list is populated from `JurisdictionLists[ConfigEnvironment]`
And there is no static `run_list`, `test_list`, or `prefix_list` field in the class
And the `is_test_list` boolean is removed

**AC-6 — Legacy flat config keys removed**
Given the refactored `Program.cs` and `appsettings.json`
When the developer searches for `data_migration:couchdb_url`, `data_migration:timer_user_name`, `data_migration:timer_value`, `data_migration:config_id`
Then none of these keys exist in any source file
And `ConfigurationSet` (CouchDB-fetched config) is no longer loaded in `Main`

**AC-7 — `has_been_done_set` skip mechanism preserved**
Given the refactored `Program.cs` still contains the `has_been_done_set` `HashSet`
When the loop processes a prefix that is in the set
Then it is skipped as before — this safety mechanism is not removed

**AC-8 — `MigrationSettings.RunType` and `IsReportOnlyMode` configurable via appsettings.local.json**
Given `appsettings.local.json` has `MigrationSettings.RunType = "DataMigration"` and `MigrationSettings.IsReportOnlyMode = true`
When the migration runs
Then `MigrationType` is set from the config value
And `is_report_only_mode` is set from `MigrationSettings.IsReportOnlyMode`
And the developer does not need to edit `Program.cs` to change these values

---

## Dev Notes — Current State and Refactor Target

### Current Structure

**`data-migration/Program.cs` (current):**
```csharp
static private IConfiguration Configuration;
static private mmria.common.couchdb.ConfigurationSet ConfigurationSet;
static private string config_timer_user_name;
static private string config_timer_value;
static private string config_couchdb_url;
// ...

// Hardcoded lists:
static List<string> run_list;
static HashSet<string> has_been_done_set = new HashSet<string>(...) { ... };
static List<string> test_list = new List<string>() { "qa" };  // many commented entries
static HashSet<string> prefix_list = new HashSet<string>() { "mp", "mh", ..., "wy" };

// In Main():
config_couchdb_url = Configuration["data_migration:couchdb_url"];
config_timer_user_name = Configuration["data_migration:timer_user_name"];
config_timer_value = Configuration["data_migration:timer_value"];
var config_id = Configuration["data_migration:config_id"];
ConfigurationSet = GetConfiguration(config_id);  // fetches from CouchDB
// ...
bool is_test_list = true;  // toggle for which list to use
bool is_report_only_mode = true;
RunTypeEnum MigrationType = RunTypeEnum.DataMigration;
if(is_test_list) run_list = test_list; else run_list = prefix_list.ToList();
```

**`data-migration/appsettings.json` (current):**
```json
{
  "mmria_settings": { ... large legacy block ... },
  "data_migration": {
    "config_id": "...",
    "couchdb_url": "...",
    "timer_user_name": "...",
    "timer_value": "...",
    "metadata_version": "..."
  }
}
```

### Target Structure

**New `data-migration/Configuration.cs`** (model after `Replication/Configuration.cs`):
```csharp
public class DataMigrationAppConfiguration
{
    public MigrationSettings MigrationSettings { get; set; } = new();
    public EnvironmentSettings EnvironmentSettings { get; set; } = new();
    public CouchDBSettings CouchDBSettings { get; set; } = new();
    public Dictionary<string, CredentialConfig> Credentials { get; set; } = new();
    public Dictionary<string, List<string>> JurisdictionLists { get; set; } = new();
}

public class MigrationSettings
{
    public string RunType { get; set; } = "DataMigration";
    public bool IsReportOnlyMode { get; set; } = true;
    public string DatabaseName { get; set; } = "mmrds";  // appended as "{prefix}{DatabaseName}"
}

// EnvironmentSettings, CouchDBSettings, DatabaseUrlTemplates, CredentialConfig
// — identical shape to Replication/Configuration.cs
```

**New `data-migration/appsettings.json`** (schema with blank defaults):
```json
{
  "MigrationSettings": {
    "RunType": "DataMigration",
    "IsReportOnlyMode": true,
    "DatabaseName": "mmrds"
  },
  "EnvironmentSettings": {
    "ConfigEnvironment": "QA",
    "IntOrProd": ""
  },
  "CouchDBSettings": {
    "DatabaseUrlTemplates": {
      "Localhost": "http://{prefix}-couchdb.local:6984",
      "Development": "https://couchdb-{prefix}-mmria.apps.ecpaas-dev.cdc.gov",
      "QA": "https://couchdb-{prefix}-mmria.apps.ecpaas-dev.cdc.gov",
      "Integration": "https://couchdb-mmria-{prefix}-int.apps.ecpaas.cdc.gov/",
      "Production": "https://couchdb-mmria-{prefix}.apps.ecpaas.cdc.gov"
    }
  },
  "Credentials": {
    "Localhost": { "Username": "", "Password": "" },
    "Development": { "Username": "", "Password": "" },
    "QA": { "Username": "", "Password": "" },
    "Integration": { "Username": "", "Password": "" },
    "Production": { "Username": "", "Password": "" }
  },
  "JurisdictionLists": {
    "Localhost": [],
    "Development": [],
    "QA": [],
    "Integration": [],
    "Production": [],
    "Alternate": [],
    "Filtered": []
  }
}
```

**`appsettings.local.json`** (gitignored template — developer fills in):
```json
{
  "MigrationSettings": {
    "RunType": "DataMigration",
    "IsReportOnlyMode": true
  },
  "EnvironmentSettings": {
    "ConfigEnvironment": "QA"
  },
  "Credentials": {
    "QA": { "Username": "mmrds", "Password": "" }
  },
  "JurisdictionLists": {
    "QA": ["qa", "test"]
  }
}
```

**Refactored `Program.cs` startup (key changes):**
```csharp
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false);

var rawConfig = builder.Build();
var config = rawConfig.Get<DataMigrationAppConfiguration>();

var configEnv = config.EnvironmentSettings.ConfigEnvironment;
var runList = config.JurisdictionLists.ContainsKey(configEnv)
    ? config.JurisdictionLists[configEnv]
    : new List<string>();

bool is_report_only_mode = config.MigrationSettings.IsReportOnlyMode;
RunTypeEnum MigrationType = Enum.Parse<RunTypeEnum>(config.MigrationSettings.RunType);

// URL and credential per prefix:
var urlTemplate = config.CouchDBSettings.DatabaseUrlTemplates.GetTemplate(configEnv);
var creds = config.Credentials.ContainsKey(configEnv) ? config.Credentials[configEnv] : null;

foreach (var prefix in runList)
{
    if (has_been_done_set.Contains(prefix)) continue;
    var db_url = urlTemplate.Replace("{prefix}", prefix);
    var db_name = $"{prefix}{config.MigrationSettings.DatabaseName}";
    var username = creds?.Username;
    var password = creds?.Password;
    // ...
}
```

### .gitignore Update

Ensure `appsettings.local.json` is in the `.gitignore` for `nccdphp-drh-mmria-utilities`. Check `c:\repos\nccdphp-drh-mmria-utilities\.gitignore` — if it doesn't have an entry for `appsettings.local.json`, add one.

### Files to Change

| File | Change |
|------|--------|
| `nccdphp-drh-mmria-utilities/data-migration/Configuration.cs` | **New file** — `DataMigrationAppConfiguration` and supporting classes |
| `nccdphp-drh-mmria-utilities/data-migration/appsettings.json` | **Replace** — new structured schema, remove legacy `mmria_settings` / `data_migration` flat keys |
| `nccdphp-drh-mmria-utilities/data-migration/appsettings.local.json` | **New file** (gitignored) — developer-local credentials and active settings |
| `nccdphp-drh-mmria-utilities/data-migration/Program.cs` | **Refactor** — remove hardcoded lists, load from config, remove `ConfigurationSet` CouchDB fetch, read `RunType` and `IsReportOnlyMode` from config |
| `nccdphp-drh-mmria-utilities/.gitignore` (or root `.gitignore`) | Add `appsettings.local.json` if not already present |

### Reference: Replication Project Files

Use these as the direct model:
- `c:\repos\nccdphp-drh-mmria-utilities\Replication\Configuration.cs`
- `c:\repos\nccdphp-drh-mmria-utilities\Replication\appsettings.json`
- `c:\repos\nccdphp-drh-mmria-utilities\Replication\appsettings.local.json`
- `c:\repos\nccdphp-drh-mmria-utilities\Replication\Program.cs` (lines ~560–650 show config loading pattern)

### Sequencing

- Story 12.2 depends on this story — do this first.
- Independent of Story 11.1 (different project).

---

## Dev Agent Record

_To be completed by dev agent after implementation._

### Completion Notes

### Change Log

| File | Change |
|------|--------|
| | |
