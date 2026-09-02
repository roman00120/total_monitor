# TOV452 y Modbus RS485

La aplicación contiene el transporte Modbus genérico, CRC16, timeouts, reintentos y simulación. No contiene registros específicos del TOV452.

El catálogo lógico de variables está en Core, en Tov452MeasurementCatalog. Incluye Voltaje, Corriente, Potencia activa/reactiva/aparente, Factor de potencia, Frecuencia, Energía, Demanda y THD de voltaje/corriente. Este catálogo describe nombres y unidades de negocio únicamente; no es un mapa Modbus y no asigna direcciones.

TOV452RegisterMap es la estructura configurable para recibir posteriormente entradas oficiales. Cada entrada deja pendientes Address, FunctionCode, DataType, Length, Scale, Offset y Endianness; el valor predeterminado es vacío y no habilita lecturas reales.

## Vocabulario de integración

Modbus RTU sobre RS-485 usa un Slave ID para identificar el equipo. La futura configuración distinguirá Register Address, Holding Register, Input Register y Function Code. Baudrate y Parity serán parámetros configurables del enlace.

Las variables de aplicación incluyen V1, V2, V3; I1, I2, I3; P1, P2, P3; PF; Frequency; Energy y THD. Sus correspondencias con registros físicos, unidades, escalas y tipos se cargarán únicamente desde el mapa oficial del TOV452.

Antes de activar producción se debe obtener del fabricante el mapa de registros, funciones, tipos, escalas, unidades, endianness, dirección esclava, parámetros seriales y procedimiento de aceptación. Esos datos deben aprobarse y probarse con un equipo real. Nunca se deben inferir direcciones o presentar datos simulados como reales.

La prueba de comunicación debe documentar COM, baud rate, paridad, bits de datos, bits de parada, terminación/bias RS485, dirección y evidencia de respuestas/CRC.

La integración futura debe construir MeterRegisterMap con RegisterGroup y ModbusRegisterDefinition aprobados para TOV452. Hasta entonces MeterRegisterMap.Empty debe permanecer activo para impedir lecturas ficticias.
