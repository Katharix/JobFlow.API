<#
.SYNOPSIS
    Syncs the local JobFlow database from the Azure staging environment.

.DESCRIPTION
    Exports the staging database as a .bacpac from Azure using SqlPackage,
    drops the local JobFlow database, and imports the .bacpac locally.
    The staging connection string is retrieved from Azure Key Vault.

.PARAMETER Manual
    Run in interactive mode with confirmation prompts.

.PARAMETER SkipExport
    Skip the export step and use an existing .bacpac file.

.PARAMETER BacpacPath
    Path to an existing .bacpac file to import (implies -SkipExport).

.EXAMPLE
    .\Sync-StagingDb.ps1
    .\Sync-StagingDb.ps1 -Manual
    .\Sync-StagingDb.ps1 -SkipExport -BacpacPath "C:\backups\staging.bacpac"
#>

[CmdletBinding()]
param(
    [switch]$Manual,
    [switch]$SkipExport,
    [string]$BacpacPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Configuration ───────────────────────────────────────────────────────────
$KeyVaultName       = "jobflow-staging"
$SecretName         = "JobFlowDB"
$LocalServer        = "."
$LocalDatabase      = "JobFlow"
$BackupDir          = Join-Path $PSScriptRoot "backups"
$LogDir             = Join-Path $PSScriptRoot "logs"
$Timestamp          = Get-Date -Format "yyyy-MM-dd_HHmmss"
$DefaultBacpacPath  = Join-Path $BackupDir "JobFlow_Staging_$Timestamp.bacpac"
$LogFile            = Join-Path $LogDir "sync_$Timestamp.log"
$RetentionDays      = 30

# ── Helpers ─────────────────────────────────────────────────────────────────

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $entry = "[$Level] $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message"
    Write-Host $entry -ForegroundColor $(switch ($Level) {
        "ERROR" { "Red" }
        "WARN"  { "Yellow" }
        "OK"    { "Green" }
        default { "Cyan" }
    })
    Add-Content -Path $LogFile -Value $entry
}

function Assert-Tool {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Write-Log "$Name is not installed or not in PATH." "ERROR"
        Write-Log "Install it and retry. See README.md for prerequisites." "ERROR"
        exit 1
    }
}

function Clean-OldBackups {
    if (Test-Path $BackupDir) {
        Get-ChildItem $BackupDir -Filter "*.bacpac" |
            Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$RetentionDays) } |
            ForEach-Object {
                Write-Log "Removing old backup: $($_.Name)" "INFO"
                Remove-Item $_.FullName -Force
            }
    }
}

# ── Pre-flight ──────────────────────────────────────────────────────────────

New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
New-Item -ItemType Directory -Path $LogDir    -Force | Out-Null

Write-Log "=== JobFlow Staging DB Sync Started ==="

Assert-Tool "az"
Assert-Tool "SqlPackage"

# Verify Azure CLI login
Write-Log "Verifying Azure CLI session..."
$azAccount = az account show 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Log "Not logged into Azure CLI. Running 'az login'..." "WARN"
    az login
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Azure login failed." "ERROR"
        exit 1
    }
}
Write-Log "Azure CLI session active." "OK"

# ── Retrieve staging connection string from Key Vault ───────────────────────

Write-Log "Retrieving staging connection string from Key Vault '$KeyVaultName'..."
$StagingConnStr = az keyvault secret show `
    --vault-name $KeyVaultName `
    --name $SecretName `
    --query "value" -o tsv 2>&1

if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($StagingConnStr)) {
    # Fallback: try ConnectionStrings--JobFlowDB format
    Write-Log "Direct secret '$SecretName' not found, trying 'ConnectionStrings--JobFlowDB'..." "WARN"
    $StagingConnStr = az keyvault secret show `
        --vault-name $KeyVaultName `
        --name "ConnectionStrings--JobFlowDB" `
        --query "value" -o tsv 2>&1

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($StagingConnStr)) {
        Write-Log "Failed to retrieve staging connection string from Key Vault." "ERROR"
        exit 1
    }
}
Write-Log "Staging connection string retrieved." "OK"

# ── Confirmation (manual mode) ──────────────────────────────────────────────

if ($Manual) {
    Write-Host ""
    Write-Host "This will:" -ForegroundColor Yellow
    Write-Host "  1. Export the staging database as a .bacpac" -ForegroundColor Yellow
    Write-Host "  2. DROP your local '$LocalDatabase' database" -ForegroundColor Yellow
    Write-Host "  3. Import the staging .bacpac into your local SQL Server" -ForegroundColor Yellow
    Write-Host ""
    $confirm = Read-Host "Continue? (y/N)"
    if ($confirm -notin @("y", "Y", "yes")) {
        Write-Log "Aborted by user." "WARN"
        exit 0
    }
}

# ── Step 1: Export staging DB ───────────────────────────────────────────────

if ($BacpacPath) {
    $SkipExport = $true
    $DefaultBacpacPath = $BacpacPath
}

if (-not $SkipExport) {
    Write-Log "Exporting staging database to $DefaultBacpacPath ..."
    Write-Log "(This may take several minutes depending on DB size)"

    SqlPackage /Action:Export `
        /SourceConnectionString:"$StagingConnStr" `
        /TargetFile:"$DefaultBacpacPath" `
        /p:CommandTimeout=300

    if ($LASTEXITCODE -ne 0) {
        Write-Log "SqlPackage export failed." "ERROR"
        exit 1
    }
    Write-Log "Export complete: $DefaultBacpacPath" "OK"
} else {
    # When -SkipExport is used without -BacpacPath, find the most recent .bacpac
    if (-not (Test-Path $DefaultBacpacPath)) {
        $latest = Get-ChildItem $BackupDir -Filter "*.bacpac" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latest) {
            $DefaultBacpacPath = $latest.FullName
            Write-Log "Auto-detected latest bacpac: $DefaultBacpacPath" "INFO"
        } else {
            Write-Log "No .bacpac files found in $BackupDir. Run without -SkipExport first." "ERROR"
            exit 1
        }
    }
    Write-Log "Using existing bacpac: $DefaultBacpacPath" "INFO"
}

# ── Step 2: Drop local database ────────────────────────────────────────────

Write-Log "Dropping local database '$LocalDatabase' (if exists)..."

$dropSql = @"
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'$LocalDatabase')
BEGIN
    ALTER DATABASE [$LocalDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$LocalDatabase];
END
"@

sqlcmd -S $LocalServer -C -Q $dropSql -b
if ($LASTEXITCODE -ne 0) {
    Write-Log "Failed to drop local database. Ensure no active connections." "ERROR"
    exit 1
}
Write-Log "Local database dropped." "OK"

# ── Step 3: Import .bacpac locally ──────────────────────────────────────────

Write-Log "Importing .bacpac into local SQL Server..."

SqlPackage /Action:Import `
    /SourceFile:"$DefaultBacpacPath" `
    /TargetConnectionString:"Server=$LocalServer;Database=$LocalDatabase;Integrated Security=True;TrustServerCertificate=True;" `
    /p:CommandTimeout=300

if ($LASTEXITCODE -ne 0) {
    Write-Log "SqlPackage import failed." "ERROR"
    exit 1
}
Write-Log "Import complete. Local '$LocalDatabase' database is now synced with staging." "OK"

# ── Cleanup ─────────────────────────────────────────────────────────────────

Clean-OldBackups

Write-Log "=== Sync completed successfully ==="
Write-Log "Bacpac: $DefaultBacpacPath"
Write-Log "Log:    $LogFile"
