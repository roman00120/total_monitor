param([string]$Environment = 'Production')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$env:ASPNETCORE_ENVIRONMENT = $Environment
dotnet ef database update --project (Join-Path $root 'src/TotalMonitor.Infrastructure') --startup-project (Join-Path $root 'src/TotalMonitor.Server')
