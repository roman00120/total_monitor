param(
    [string]$InstallDirectory = 'C:\Program Files\TotalMonitor'
)

$ErrorActionPreference = 'Stop'

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Instalando Servicio Windows Total Monitor " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$exe = Join-Path $InstallDirectory 'TotalMonitor.Server.exe'
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Error: No se encontró el ejecutable del servidor en $exe. Asegúrese de haber instalado o copiado los archivos en $InstallDirectory."
}

Write-Host "Verificando servicio previo..." -ForegroundColor Gray
$existingService = Get-Service -Name TotalMonitor -ErrorAction SilentlyContinue
if ($null -ne $existingService) {
    Write-Host "Deteniendo y eliminando servicio TotalMonitor previo..." -ForegroundColor Yellow
    & sc.exe stop TotalMonitor *>$null
    Start-Sleep -Seconds 2
    & sc.exe delete TotalMonitor *>$null
    Start-Sleep -Seconds 1
}

$quotedExe = '"' + $exe + '"'
Write-Host "Registrando servicio TotalMonitor con ruta: $quotedExe..." -ForegroundColor Gray
& sc.exe create TotalMonitor binPath= $quotedExe start= auto DisplayName= "Total Monitor"

if ($LASTEXITCODE -ne 0) {
    throw "Error al registrar el servicio con sc.exe (Código $LASTEXITCODE). Asegúrese de ejecutar PowerShell como Administrador."
}

Write-Host "Configurando acciones de recuperación automática ante fallos..." -ForegroundColor Gray
& sc.exe failure TotalMonitor reset= 86400 actions= restart/5000/restart/30000/restart/60000

Write-Host "Iniciando servicio TotalMonitor..." -ForegroundColor Gray
& sc.exe start TotalMonitor

Write-Host "Verificando salud del servicio y API en http://localhost:5080/api/v1/health..." -ForegroundColor Gray
$healthy = $false
for ($i = 1; $i -le 15; $i++) {
    Start-Sleep -Seconds 1
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5080/api/v1/health" -Method Get -TimeoutSec 2 -ErrorAction Stop
        if ($response.status -eq "ready" -or $response.status -eq "degraded") {
            $healthy = $true
            Write-Host "✓ API Total Monitor activa y respondiendo correctamente (Estado: $($response.status), Servidor: $($response.serverMode))." -ForegroundColor Green
            break
        }
    }
    catch {
        Write-Host "Esperando inicio del servidor... (intento $i/15)" -ForegroundColor DarkGray
    }
}

if (-not $healthy) {
    Write-Warning "El servicio inició pero el endpoint de salud no respondió en 15 segundos. Revise los registros del visor de eventos de Windows y MySQL."
} else {
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host " Servicio Total Monitor instalado con éxito!" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Green
}
