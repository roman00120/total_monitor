param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
$uri = $BaseUrl.TrimEnd('/') + '/api/v1/health'
try {
    $result = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 10
    $result | ConvertTo-Json -Depth 5
    if ($result.status -eq 'degraded') { exit 2 }
} catch {
    Write-Error ("Health check failed: " + $_.Exception.Message)
    exit 1
}
