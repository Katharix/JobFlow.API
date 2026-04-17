<#
.SYNOPSIS
    Registers (or removes) a Windows Task Scheduler job that runs Sync-StagingDb.ps1 every two weeks.

.PARAMETER Register
    Create/update the scheduled task.

.PARAMETER Unregister
    Remove the scheduled task.

.PARAMETER DayOfWeek
    Day of the week to run. Default: Sunday.

.PARAMETER Time
    Time of day to run. Default: 02:00 (2 AM).

.EXAMPLE
    .\Register-ScheduledSync.ps1 -Register
    .\Register-ScheduledSync.ps1 -Register -DayOfWeek Monday -Time "06:00"
    .\Register-ScheduledSync.ps1 -Unregister
#>

[CmdletBinding()]
param(
    [switch]$Register,
    [switch]$Unregister,
    [string]$DayOfWeek = "Sunday",
    [string]$Time = "02:00"
)

$ErrorActionPreference = "Stop"

$TaskName        = "JobFlow-SyncStagingDb"
$TaskDescription = "Syncs local JobFlow database from Azure staging every two weeks."
$ScriptPath      = Join-Path $PSScriptRoot "Sync-StagingDb.ps1"

if ($Unregister) {
    if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
        Write-Host "Scheduled task '$TaskName' removed." -ForegroundColor Green
    } else {
        Write-Host "Scheduled task '$TaskName' does not exist." -ForegroundColor Yellow
    }
    return
}

if (-not $Register) {
    Write-Host "Usage: .\Register-ScheduledSync.ps1 -Register | -Unregister" -ForegroundColor Yellow
    return
}

# Verify the sync script exists
if (-not (Test-Path $ScriptPath)) {
    Write-Host "Sync script not found at: $ScriptPath" -ForegroundColor Red
    exit 1
}

# Build the action: run PowerShell with the sync script (non-interactive)
$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$ScriptPath`"" `
    -WorkingDirectory $PSScriptRoot

# Trigger: every 2 weeks on the specified day at the specified time
$trigger = New-ScheduledTaskTrigger `
    -Weekly `
    -WeeksInterval 2 `
    -DaysOfWeek $DayOfWeek `
    -At $Time

# Settings: allow start on battery, wake to run, stop if running > 2 hours
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2)

# Principal: run as current user
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType S4U -RunLevel Highest

# Remove existing task if it exists
if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "Existing task '$TaskName' removed." -ForegroundColor Yellow
}

# Register the task
Register-ScheduledTask `
    -TaskName $TaskName `
    -Description $TaskDescription `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal

Write-Host ""
Write-Host "Scheduled task '$TaskName' registered successfully." -ForegroundColor Green
Write-Host "  Schedule : Every 2 weeks on $DayOfWeek at $Time" -ForegroundColor Cyan
Write-Host "  Script   : $ScriptPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run it immediately:  Start-ScheduledTask -TaskName '$TaskName'" -ForegroundColor Cyan
Write-Host "To view it:             Get-ScheduledTask -TaskName '$TaskName' | Format-List" -ForegroundColor Cyan
