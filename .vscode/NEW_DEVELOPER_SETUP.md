# New Developer Setup Guide

## Prerequisites

1. **.NET 9.0 SDK** - https://dotnet.microsoft.com/download/dotnet/9.0
2. **Git**
3. **Visual Studio Code**
4. **CouchDB** running locally (or Docker)

## Step 1: Clone Required Repositories

Clone the **3 required repositories** into the **same parent directory**:

```bash
# Navigate to your source folder
cd ~/source/repos  # or C:\Users\{you}\source\repos on Windows

# Clone required repos
git clone https://github.com/CDCgov/nccdphp-drh-mmria.git
git clone https://github.com/CDCgov/nccdphp-drh-mmria-services.git
git clone https://github.com/CDCgov/nccdphp-drh-mmria-common.git
```

**Important:** The 3 required repos must be siblings in the same folder for subtree push/pull tasks to work:

```
source/repos/
  ├── nccdphp-drh-mmria/           (main server) ✅ Required
  ├── nccdphp-drh-mmria-services/  (background services) ✅ Required
  ├── nccdphp-drh-mmria-common/    (shared libraries) ✅ Required
  └── nccdphp-drh-mmria-utilities/ (tools) ⚪ Optional
```

## Step 2: Open Workspace in VS Code

```bash
cd nccdphp-drh-mmria
code .vscode/mmria-server-services.code-workspace
```

This opens the multi-root workspace with all 4 folders.

## Step 3: Set Up CouchDB

### Option A: Docker (Recommended)

```bash
docker run -d --name couchdb \
  -p 5984:5984 \
  -e COUCHDB_USER=mmrds \
  -e COUCHDB_PASSWORD=mmrds \
  couchdb:latest
```

### Option B: Local Install

- Download from https://couchdb.apache.org/
- Create user: `mmrds` / `mmrds`
- Ensure accessible at http://localhost:5984

### Create Configuration Document

```bash
# Create configuration database
curl -X PUT http://mmrds:mmrds@localhost:5984/configuration

# Create localhost configuration (copy from another dev or create new)
curl -X PUT http://mmrds:mmrds@localhost:5984/configuration/localhost \
  -H "Content-Type: application/json" \
  -d @path/to/localhost-config.json
```

## Step 4: Create Local Configuration Files

Create `appsettings.local.json` in both projects:

**mmria-server:** `source-code/mmria/mmria-server/appsettings.local.json`

```json
{
  "mmria_settings": {
    "is_environment_based": "false",
    "timer_user_name": "mmrds",
    "timer_value": "mmrds",
    "couchdb_url": "http://localhost:5984"
  }
}
```

**mmria-services:** `mmria.services/appsettings.local.json`

```json
{
  "mmria_settings": {
    "is_environment_based": "false",
    "timer_user_name": "mmrds",
    "timer_password": "mmrds",
    "config_id": "localhost",
    "couchdb_url": "http://localhost:5984"
  }
}
```

These files are git-ignored and safe for credentials.

## Step 5: Restore & Build

In VS Code:

1. Press `Ctrl+Shift+B` → Select "build-both"
2. Or run in terminal:
   ```bash
   dotnet restore
   dotnet build
   ```

## Step 6: Launch Applications

1. Press `F5`
2. Select **"Launch Both (Server + Services)"** from dropdown
3. Both applications will start in debug mode

Or launch individually:

- **mmria-server** runs on port 8080
- **mmria-services** runs on port 44331

## Subtree Tasks

The workspace includes tasks to sync the `common` folder (shared libraries) with the standalone nccdphp-drh-mmria-common repo:

### Pull Changes FROM common repo:

- Run Task: `🔄 Pull Common Subtree - Server`
- Run Task: `🔄 Pull Common Subtree - Services`

### Push Changes TO common repo:

- Run Task: `⬆️ Push Common Subtree - Server`
- Run Task: `⬆️ Push Common Subtree - Services`

**Note:** These tasks work because all repos are in the same parent directory.

## Common Issues

### "config_id not found" or "cron_schedule not found"

- Ensure appsettings.local.json has `config_id: "localhost"`
- Verify CouchDB configuration/localhost document exists

### Subtree tasks fail

- Verify all 4 repos are cloned in same parent folder
- Check you have push access to nccdphp-drh-mmria-common repo

### Port conflicts

- mmria-server: Change port in appsettings
- mmria-services: Change port 44331 in appsettings

### NullReferenceException in BatchSupervisor

- Ensure CouchDB is running
- Check credentials in appsettings.local.json

## Getting Help

See [WORKSPACE_SETUP.md](WORKSPACE_SETUP.md) for detailed configuration and troubleshooting.
