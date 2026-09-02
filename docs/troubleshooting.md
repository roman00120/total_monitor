# Diagnóstico y solución de problemas

## El cliente no conecta

Confirme Api:BaseUrl, DNS/firewall y el certificado HTTPS. Pruebe /api/v1/health desde el mismo equipo cliente.

## Salud degraded

La API está activa pero no conecta a MySQL. Revise la cadena de conexión, credenciales, servicio MySQL y migraciones.

## Sin mediciones

No active adquisición sin mapa oficial TOV452. Revise Acquisition:Enabled, Modbus:Enabled, COM, parámetros seriales y permisos del usuario del servicio.

## Servicio detenido

Consulte el Visor de eventos de Windows y ejecute sc.exe query TotalMonitor. La política de recuperación reinicia el proceso tras fallos.

## Backup

Crear: powershell -File .\scripts\mysql-backup.ps1 -OutputDirectory .\backups. Restaurar requiere validar el archivo y una ventana de mantenimiento: powershell -File .\scripts\mysql-restore.ps1 -BackupFile .\backups\archivo.sql.
