param([ValidateSet('client','server','all')][string]$Target = 'all', [string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $root 'publish'
New-Item -ItemType Directory -Force -Path $publish | Out-Null
if ($Target -in @('client','all')) { dotnet publish (Join-Path $root 'src/TotalMonitor.App/TotalMonitor.App.csproj') -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o (Join-Path $publish 'client') }
if ($Target -in @('server','all')) { dotnet publish (Join-Path $root 'src/TotalMonitor.Server/TotalMonitor.Server.csproj') -c $Configuration -r win-x64 --self-contained true -o (Join-Path $publish 'server') }
Write-Host "Published artifacts are in $publish"
