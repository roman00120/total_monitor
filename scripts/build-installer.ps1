param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = (Get-Item $PSScriptRoot).Parent.FullName

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Construccion del Instalador Total Monitor " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

Set-Location $root

Write-Host "`n1. Ejecutando suite de pruebas automatizadas..." -ForegroundColor Yellow
dotnet test TotalMonitor.slnx -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Fallaron las pruebas unitarias. Abortando construccion."
}

Write-Host "`n2. Compilando solucion completa en modo $Configuration..." -ForegroundColor Yellow
dotnet build TotalMonitor.slnx -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Fallo la compilacion de la solucion."
}

Write-Host "`n3. Publicando Cliente y Servidor (self-contained win-x64)..." -ForegroundColor Yellow
& powershell -ExecutionPolicy Bypass -File (Join-Path $root 'scripts\publish.ps1') -Target all -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Fallo la publicacion de artefactos."
}

$publishDir = Join-Path $root 'publish'
$installerDir = Join-Path $publishDir 'installer'
if (-not (Test-Path $installerDir)) {
    New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
}

Write-Host "`n4. Verificando compilador de Inno Setup (ISCC)..." -ForegroundColor Yellow

$isccExe = $null
if (Get-Command iscc -ErrorAction SilentlyContinue) {
    $isccExe = "iscc"
}
if (($null -eq $isccExe) -and (Test-Path "C:\Program Files (x86)\Inno Setup 6\ISCC.exe")) {
    $isccExe = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}
if (($null -eq $isccExe) -and (Test-Path "C:\Program Files\Inno Setup 6\ISCC.exe")) {
    $isccExe = "C:\Program Files\Inno Setup 6\ISCC.exe"
}

if ($null -ne $isccExe) {
    Write-Host "Compilando instalador oficial con $isccExe..." -ForegroundColor Green
    & $isccExe (Join-Path $root 'installer\TotalMonitor.iss')
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Instalador TotalMonitor-Setup.exe generado exitosamente en $installerDir" -ForegroundColor Green
    }
}
else {
    Write-Host "Inno Setup no esta instalado en el PATH actual." -ForegroundColor DarkYellow
    Write-Host "Empaquetando distribucion lista para produccion en archivo ZIP..." -ForegroundColor Gray

    $zipPath = Join-Path $installerDir 'TotalMonitor-Standalone-Package.zip'
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    $clientDir = Join-Path $publishDir 'client'
    $serverDir = Join-Path $publishDir 'server'
    $docsDir = Join-Path $root 'docs'
    $scriptsDir = Join-Path $root 'scripts'

    Compress-Archive -Path $clientDir, $serverDir, $docsDir, $scriptsDir -DestinationPath $zipPath -Force
    Write-Host "[OK] Paquete independiente de distribucion generado en: $zipPath" -ForegroundColor Green
}

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host " Construccion finalizada con exito!" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
