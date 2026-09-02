param([string]$HostName = 'localhost', [string]$Database = 'totalmonitor', [string]$User = 'root', [string]$OutputDirectory = '.\backups')
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$file = Join-Path $OutputDirectory ("totalmonitor-{0:yyyyMMdd-HHmmss}.sql" -f (Get-Date))
mysqldump --host=$HostName --user=$User --databases $Database --result-file=$file
Write-Host "Backup created at $file"
