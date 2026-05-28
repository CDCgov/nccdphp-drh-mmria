# MMRIA Multi-Project Workspace Setup

This workspace contains three core repositories:

- **nccdphp-drh-mmria** (main) - mmria-server
- **nccdphp-drh-mmria-services** - background services
- **nccdphp-drh-mmria-common** - shared libraries

Optional:

- **nccdphp-drh-mmria-utilities** - utility tools (data-migration, mmria-console, etc.)

## Quick Start

### Prerequisites

1. **.NET 9.0 SDK** installed
2. **CouchDB** running at `http://localhost:5984`
   - Default credentials: `mmrds:mmrds`
   - Configuration database with `localhost` document

### Launch Configurations

Use the **Run and Debug** panel (Ctrl+Shift+D) to select:

1. **Launch Both (Server + Services)** - Starts both projects together (recommended)
2. **Launch mmria-server** - Start only the main server
3. **Launch mmria-services** - Start only the background services

### Build Tasks

Available in **Terminal > Run Task**:

- `build-server` - Build mmria-server only
- `build-services` - Build mmria-services only
- `build-both` - Build both projects (default)

## Project Details

### mmria-server

- **Location**: `source-code/mmria/mmria-server/`
- **Port**: 8080 (or as configured)
- **Purpose**: Main web application and API

### mmria-services

- **Location**: `mmria.services/`
- **Port**: 44331
- **Purpose**: Background worker service (Akka actors, scheduled jobs)
- **Key Features**:
  - Vitals import processing
  - Backup operations
  - CDC instance population
  - Batch processing

## Configuration

### Local Configuration Files (New!)

Both projects now support `appsettings.local.json` for local development with credentials:

- **mmria-server:** `source-code/mmria/mmria-server/appsettings.local.json`
- **mmria-services:** `mmria.services/appsettings.local.json`

These files are:

- ✓ Automatically loaded by ASP.NET Core
- ✓ Excluded from git (won't be committed)
- ✓ Override settings from appsettings.json

Add your local credentials here:

```json
{
  "mmria_settings": {
    "timer_user_name": "your-username",
    "timer_password": "your-password",
    "timer_value": "your-password"
  }
}
```

### mmria-server

Configuration: `source-code/mmria/mmria-server/appsettings.json`

- CouchDB: `http://localhost:5984`
- Vitals service URL: `http://localhost:44331/api/Message/IJESet`

### mmria-services

Configuration: `mmria.services/appsettings.json`

- CouchDB: `http://localhost:5984`
- Config ID: `localhost` (references CouchDB configuration document)
- Cron schedule: `0 */1 * * * ?` (every minute)

## Running Manually

### From Terminal

**mmria-server:**

```bash
cd source-code/mmria/mmria-server
dotnet run
```

**mmria-services:**

```bash
cd ../../nccdphp-drh-mmria-services/mmria.services
dotnet run
```

## Shared Dependencies

Both projects reference the common libraries:

- `mmria.common` - Core domain models and utilities
- `mmria.getset` - Data access helpers

These are shared via git subtrees and should be kept in sync across repositories.

## Troubleshooting

### mmria-services won't start

- Verify CouchDB is running: `curl http://localhost:5984`
- Check config_id in appsettings.json points to existing document
- Ensure configuration document has `cron_schedule` field

### Port conflicts

- mmria-server: Change port in appsettings.json
- mmria-services: Change port 44331 in appsettings.json

### Build errors

- Run `dotnet restore` in each project directory
- Ensure all workspace folders are loaded
- Check .NET 9.0 SDK is installed: `dotnet --version`
