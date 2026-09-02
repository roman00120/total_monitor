$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$server = Join-Path $root 'publish\server\TotalMonitor.Server.exe'
$client = Join-Path $root 'publish\client\TotalMonitor.exe'
if (-not (Test-Path -LiteralPath $server)) { throw "No se encontró el servidor publicado: $server" }
if (-not (Test-Path -LiteralPath $client)) { throw "No se encontró el cliente publicado: $client" }

$mysqlPassword = Read-Host 'Contraseña de MySQL para root (dejar vacía si no tiene)'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:TOTALMONITOR_ConnectionStrings__Default = "Server=localhost;Port=3306;Database=totalmonitor;User=root;Password=$mysqlPassword;"
$serverProcess = Start-Process -FilePath $server -WorkingDirectory (Split-Path $server) -PassThru
try {
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        Start-Sleep -Seconds 1
        try {
            $health = Invoke-RestMethod 'http://localhost:5080/api/v1/health' -TimeoutSec 2
            if ($health.status -in @('ready','degraded')) { Start-Process -FilePath $client; exit 0 }
        } catch {
            if ($serverProcess.HasExited) { throw 'El servidor terminó durante la inicialización. Revise el mensaje de error de la ventana del servidor.' }
        }
    }
    throw 'El servidor no respondió dentro del tiempo esperado.'
} finally {
    Remove-Item Env:TOTALMONITOR_ConnectionStrings__Default -ErrorAction SilentlyContinue
}
