param([Parameter(Mandatory=$true)][string]$BackupFile, [string]$HostName = 'localhost', [string]$User = 'root')
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $BackupFile)) { throw "Backup file not found: $BackupFile" }
Get-Content -LiteralPath $BackupFile -Raw | mysql --host=$HostName --user=$User
Write-Host "Restore completed. Existing data was not deleted by this script."
