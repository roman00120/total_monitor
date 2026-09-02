$ErrorActionPreference = 'Stop'
sc.exe stop TotalMonitor
sc.exe delete TotalMonitor
Write-Host 'TotalMonitor service removed. Database files were not deleted.'
