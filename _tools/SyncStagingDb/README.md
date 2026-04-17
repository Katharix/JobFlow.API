# SyncStagingDb

Syncs your local `JobFlow` SQL Server database from the Azure staging environment using SqlPackage `.bacpac` export/import.

## Prerequisites

| Tool | Install |
|------|---------|
| **Azure CLI** | `winget install Microsoft.AzureCLI` |
| **SqlPackage** | `winget install Microsoft.SqlPackage` or [download](https://learn.microsoft.com/sql/tools/sqlpackage/sqlpackage-download) |
| **sqlcmd** | Included with SQL Server. Also: `winget install Microsoft.SqlServer.SqlCmd` |
| **Local SQL Server** | Express/Developer edition with Windows auth on default instance (`.`) |

You must have **read access** to the `jobflow-staging` Azure Key Vault.

## Quick Start

### Manual sync

```powershell
cd _tools\SyncStagingDb
.\Sync-StagingDb.ps1 -Manual
```

### Non-interactive sync

```powershell
.\Sync-StagingDb.ps1
```

### Import from an existing .bacpac

```powershell
.\Sync-StagingDb.ps1 -BacpacPath "C:\path\to\existing.bacpac"
```

## Scheduling (every 2 weeks)

Register a Windows Task Scheduler job (requires elevated PowerShell):

```powershell
.\Register-ScheduledSync.ps1 -Register
```

Customize day/time:

```powershell
.\Register-ScheduledSync.ps1 -Register -DayOfWeek Monday -Time "06:00"
```

Remove the scheduled task:

```powershell
.\Register-ScheduledSync.ps1 -Unregister
```

## What it does

1. Logs into Azure CLI (if not already authenticated)
2. Retrieves the staging DB connection string from Azure Key Vault (`jobflow-staging`)
3. Exports the staging database as a `.bacpac` via `SqlPackage /Action:Export`
4. Drops the local `JobFlow` database
5. Imports the `.bacpac` via `SqlPackage /Action:Import`
6. Cleans up backups older than 30 days

## Files

| File | Purpose |
|------|---------|
| `Sync-StagingDb.ps1` | Main sync script |
| `Register-ScheduledSync.ps1` | Task Scheduler registration |
| `backups/` | Exported `.bacpac` files (auto-created) |
| `logs/` | Sync run logs (auto-created) |
