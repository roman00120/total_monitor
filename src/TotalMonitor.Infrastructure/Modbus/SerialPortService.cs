using System.IO.Ports;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Modbus;

namespace TotalMonitor.Infrastructure.Modbus;

public sealed class SerialPortService(ModbusConnectionOptions options) : ISerialPortService
{
    private SerialPort? port;
    public bool IsOpen => port?.IsOpen == true;
    public string PortName => options.ComPort;

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        options.Validate();
        if (string.IsNullOrWhiteSpace(options.ComPort))
            throw new ModbusException(ModbusErrorKind.PortError, "No se ha configurado ningún puerto COM.");

        port = new SerialPort(
            options.ComPort,
            options.BaudRate,
            Enum.Parse<Parity>(options.Parity, true),
            options.DataBits,
            Enum.Parse<StopBits>(options.StopBits, true))
        {
            ReadTimeout = options.ReadTimeout,
            WriteTimeout = options.WriteTimeout
        };

        port.Open();
        return Task.CompletedTask;
    }

    public Task CloseAsync()
    {
        if (port?.IsOpen == true) port.Close();
        return Task.CompletedTask;
    }

    public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        port!.BaseStream.Write(data.Span);
        return Task.CompletedTask;
    }

    public Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        return port!.BaseStream.ReadAsync(buffer, cancellationToken).AsTask();
    }

    public ValueTask DisposeAsync()
    {
        port?.Dispose();
        port = null;
        return ValueTask.CompletedTask;
    }

    private void EnsureOpen()
    {
        if (!IsOpen) throw new ModbusException(ModbusErrorKind.PortError, "El puerto serial no está abierto.");
    }

    public static IReadOnlyList<string> GetAvailablePorts() => SerialPort.GetPortNames().OrderBy(x => x).ToArray();

    public static (bool Success, string Message, MeterHardwareState State) TestMeterModbusCommunication(
        string comPort,
        byte slaveAddress,
        int baudRate = 9600,
        string parity = "None",
        int dataBits = 8,
        string stopBits = "One",
        int timeout = 1000)
    {
        if (string.IsNullOrWhiteSpace(comPort))
            return (false, "No se especificó un puerto COM para probar.", MeterHardwareState.ESPERANDO_COM);

        if (slaveAddress == 0)
            return (false, "La dirección de esclavo Modbus debe ser entre 1 y 247.", MeterHardwareState.NO_CONFIGURADO);

        if (!Enum.TryParse<Parity>(parity, true, out var parsedParity))
            return (false, $"Paridad inválida: {parity}", MeterHardwareState.NO_CONFIGURADO);

        if (!Enum.TryParse<StopBits>(stopBits, true, out var parsedStopBits))
            return (false, $"Stop bits inválidos: {stopBits}", MeterHardwareState.NO_CONFIGURADO);

        SerialPort? testPort = null;
        try
        {
            testPort = new SerialPort(comPort, baudRate, parsedParity, dataBits, parsedStopBits)
            {
                ReadTimeout = Math.Max(timeout, 500),
                WriteTimeout = Math.Max(timeout, 500)
            };

            testPort.Open();

            // Build real Modbus RTU request frame (Read Holding Registers, Addr: 0x0000, Count: 1)
            var pdu = new byte[] { slaveAddress, 0x03, 0x00, 0x00, 0x00, 0x01 };
            var crc = ModbusCrc16.Calculate(pdu);
            var requestFrame = new byte[8];
            Array.Copy(pdu, 0, requestFrame, 0, 6);
            requestFrame[6] = (byte)(crc & 0xFF);
            requestFrame[7] = (byte)((crc >> 8) & 0xFF);

            // Clear buffers before sending
            testPort.DiscardInBuffer();
            testPort.DiscardOutBuffer();

            // Send real frame over RS485
            testPort.Write(requestFrame, 0, requestFrame.Length);

            // Wait and read response
            var responseBuffer = new byte[256];
            var bytesRead = 0;
            var startTime = DateTime.UtcNow;

            while ((DateTime.UtcNow - startTime).TotalMilliseconds < testPort.ReadTimeout)
            {
                if (testPort.BytesToRead > 0)
                {
                    var chunk = testPort.Read(responseBuffer, bytesRead, responseBuffer.Length - bytesRead);
                    bytesRead += chunk;
                    if (bytesRead >= 5) // Minimum Modbus response length (slave + fn + data/err + 2 crc)
                        break;
                }
                Thread.Sleep(20);
            }

            if (bytesRead < 5)
            {
                return (false, "Medidor no encontrado (puerto serial abierto pero el medidor TOV452 no respondió en la dirección indicada).", MeterHardwareState.ESPERANDO_MEDIDOR);
            }

            var responseSlave = responseBuffer[0];
            var responseFn = responseBuffer[1];

            if (responseSlave != slaveAddress)
            {
                return (false, $"Respuesta recibida pero de dirección esclavo incorrecta ({responseSlave} vs {slaveAddress}).", MeterHardwareState.ERROR);
            }

            // Verify CRC16
            var expectedCrc = ModbusCrc16.Calculate(responseBuffer.AsSpan(0, bytesRead - 2));
            var actualCrc = (ushort)(responseBuffer[bytesRead - 2] | (responseBuffer[bytesRead - 1] << 8));

            if (expectedCrc != actualCrc)
            {
                return (false, "Error de comunicación: CRC inválido en la respuesta recibida.", MeterHardwareState.ERROR);
            }

            return (true, $"Comunicación establecida con medidor en dirección {slaveAddress}.", MeterHardwareState.CONECTADO);
        }
        catch (UnauthorizedAccessException)
        {
            return (false, $"El puerto {comPort} está en uso por otra aplicación o no se tienen permisos suficientes.", MeterHardwareState.ERROR);
        }
        catch (IOException ex)
        {
            return (false, $"Convertidor RS485 o puerto {comPort} no disponible: {ex.Message}", MeterHardwareState.ESPERANDO_COM);
        }
        catch (Exception ex)
        {
            return (false, $"Error al probar comunicación en {comPort}: {ex.Message}", MeterHardwareState.ERROR);
        }
        finally
        {
            if (testPort?.IsOpen == true)
            {
                try { testPort.Close(); } catch { }
            }
            testPort?.Dispose();
        }
    }
}
