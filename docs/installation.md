# Guía de Instalación, Configuración y Operación - TOTAL MONITOR

## 1. Requisitos del Sistema
- **Sistema Operativo**: Windows 10 / 11 / Windows Server 2016+ (64-bit).
- **Base de Datos**: MySQL 8.0+ (Local o en red).
- **Ejecutables**: Paquetes autárquicos (*self-contained* `win-x64`), no requieren instalar .NET previamente en producción.
- **Hardware RS485 (para modo real)**: Convertidor RS485 a USB / Puerto Serial COM + Medidor TOV452.
- **Modo Simulación**: Funcional de inmediato sin requerir hardware físico serial.

---

## 2. Construcción y Publicación de Artefactos

Para compilar en Release, ejecutar las pruebas y publicar tanto el cliente WPF como el servidor API:

```powershell
# Compilación, pruebas y empaquetado automático:
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1 -Configuration Release
```

Los artefactos listos para producción se generan en:
- `publish/client/`: Ejecutable `TotalMonitor.exe` y librerías cliente.
- `publish/server/`: Ejecutable `TotalMonitor.Server.exe`, API REST, SignalR y motor de adquisición.
- `publish/installer/`: Paquete de distribución `TotalMonitor-Setup.exe` o `TotalMonitor-Standalone-Package.zip`.

---

## 3. Instalación del Servicio Windows en Producción

El servidor API y el motor de adquisición central deben ejecutarse como un **Servicio de Windows** en el host donde reside la base de datos o el puerto COM RS485.

### Instalación Automática
1. Copiar el contenido de `publish/server` a `C:\Program Files\TotalMonitor`.
2. Abrir PowerShell como **Administrador** y ejecutar:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\install-service.ps1 -InstallDirectory "C:\Program Files\TotalMonitor"
   ```
3. El script registrará el servicio `TotalMonitor`, configurará inicio automático (`start= auto`), reinicio automático ante fallos (`restart/5000/restart/30000/restart/60000`) y verificará la salud de la API en `http://localhost:5080/api/v1/health`.

### Desinstalación del Servicio
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall-service.ps1
```
*Nota: La desinstalación del servicio NO elimina ni destruye la base de datos MySQL.*

---

## 4. Configuración de Base de Datos MySQL

Total Monitor incluye un sistema de arranque automático (*Bootstrapper*):
- Verifica la conectividad con el servidor MySQL.
- Crea automáticamente la base de datos `totalmonitor` con codificación `utf8mb4` si no existe.
- Aplica todas las migraciones de Entity Framework Core de forma idempotente.
- Crea el usuario Administrador inicial por defecto (`admin` / credenciales configuradas) si la tabla de usuarios está vacía.

Cadena de conexión estándar en `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=totalmonitor;User=root;Password=TuPassword;"
  }
}
```

---

## 5. Operación: Modo Simulación vs. Hardware Real

### A. Modo Simulación (MockTOV452)
1. Iniciar sesión en Total Monitor (`admin`).
2. Ir a **Configuración** ⚙.
3. En la sección **Comunicación RS485 / Modbus RTU**, marcar la casilla **Modo simulación (MockTOV452)**.
4. Pulsar **Probar comunicación** → Se valida la disponibilidad del generador sintético.
5. Pulsar **Guardar configuración**.
6. Pulsar **INICIAR ADQUISICIÓN** → El sistema comenzará a generar lecturas eléctricas realistas (Voltaje, Corriente, Potencia, PF, Frecuencia, Energía, THD), persistirlas en la base de datos MySQL y emitirlas en tiempo real hacia el Dashboard y Monitoreo.

### B. Preparación para Hardware Real (TOV452 + Convertidor RS485/USB)
1. Conectar el convertidor RS485-USB al servidor y a los bornes A(+) y B(-) del medidor TOV452.
2. Ir a **Configuración** ⚙ y pulsar **Actualizar puertos** para detectar el puerto COM asignado en Windows (ej. `COM3`).
3. Desmarcar **Modo simulación** y seleccionar el puerto `COM3`, baud rate `9600`, paridad `None`, data bits `8`, stop bits `1`.
4. Pulsar **Probar comunicación** para comprobar que el puerto COM se puede abrir físicamente en Windows.
5. Configurar el medidor en el módulo **Medidores** con su dirección Modbus esclavo (ej. `1`).
6. *Nota*: Las lecturas de registros físicos reales requerirán cargar el mapa de registros oficial en `TOV452RegisterMap.cs` una vez validada la documentación del fabricante. Mientras tanto, el sistema protege la integridad de los datos evitando lecturas ficticias sobre registros desconocidos.

---

## 6. Diagnóstico y Resolución de Problemas

| Síntoma | Causa Probable | Solución |
|---|---|---|
| **Error al iniciar sesión (401)** | Usuario o contraseña no coinciden | Verificar credenciales del administrador inicial |
| **Error de conexión al servidor (HTTP Error)** | El servicio `TotalMonitor` está detenido o el puerto 5080 está bloqueado | Verificar con `Get-Service TotalMonitor` y abrir `http://localhost:5080/api/v1/health` |
| **MySQL no disponible al arrancar** | Servidor MySQL apagado o puerto 3306 cerrado | Iniciar servicio MySQL y verificar credenciales en `appsettings.Production.json` |
| **Puerto COM ocupado o no disponible** | Otra aplicación tiene abierto el puerto serial | Cerrar programas que utilicen el puerto COM o verificar desconexión del USB |
