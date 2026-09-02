# Total Monitor: arquitectura cliente-servidor

```text
TotalMonitor.App (WPF/MVVM)
        │ HTTPS + JWT / SignalR
        ▼
TotalMonitor.Server (ASP.NET Core API)
   ├── Authentication / Authorization
   ├── HistoricalDataService / ReportService
   ├── DataAcquisitionService
   ├── Modbus RTU / RS485
   └── Entity Framework Core / MySQL
```

El cliente no referencia `TotalMonitor.Infrastructure`, no abre puertos RS485 y no accede a MySQL. El servidor es la única aplicación que debe adquirir datos y persistirlos. Los clientes consumen DTOs mediante `/api/v1` y reciben eventos de monitoreo por `/hubs/monitoring`.

El JWT contiene identidad, rol y permisos; los endpoints también validan autorización, por lo que la seguridad no depende de ocultar controles WPF. El health check (`/api/health`) valida API y base de datos, sin consultar Modbus.

Las migraciones EF y la configuración de secretos pertenecen al despliegue del servidor. El mapa de registros TOV452 continúa fuera del código hasta disponer de documentación oficial.
