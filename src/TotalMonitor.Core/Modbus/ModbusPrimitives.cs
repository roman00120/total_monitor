namespace TotalMonitor.Core.Modbus;

public enum ModbusErrorKind { Timeout, PortError, InvalidCrc, InvalidResponse, ProtocolException, IncompleteResponse, Unknown }

public sealed class ModbusException : Exception
{
    public ModbusErrorKind Kind { get; }
    public byte? ExceptionCode { get; }
    public ModbusException(ModbusErrorKind kind, string message, byte? exceptionCode = null, Exception? innerException = null) : base(message, innerException) { Kind = kind; ExceptionCode = exceptionCode; }
}

public sealed record ModbusRequest
{
    public byte SlaveAddress { get; }
    public byte FunctionCode { get; }
    public byte[] Payload { get; }
    public ModbusRequest(byte slaveAddress, byte functionCode, byte[] payload)
    { if (slaveAddress is 0 or > 247) throw new ArgumentOutOfRangeException(nameof(slaveAddress)); if (functionCode is 0 or > 127) throw new ArgumentOutOfRangeException(nameof(functionCode)); Payload = payload ?? throw new ArgumentNullException(nameof(payload)); SlaveAddress = slaveAddress; FunctionCode = functionCode; }
    public byte[] ToFrame()
    {
        var frame = new byte[2 + Payload.Length + 2]; frame[0] = SlaveAddress; frame[1] = FunctionCode; Payload.CopyTo(frame, 2); var crc = ModbusCrc16.Calculate(frame.AsSpan(0, frame.Length - 2)); frame[^2] = (byte)(crc & 0xFF); frame[^1] = (byte)(crc >> 8); return frame;
    }
}

public sealed record ModbusResponse(byte SlaveAddress, byte FunctionCode, byte[] Data)
{
    public static ModbusResponse Parse(ReadOnlySpan<byte> frame, byte expectedSlaveAddress, byte expectedFunctionCode)
    {
        if (frame.Length < 5) throw new ModbusException(ModbusErrorKind.IncompleteResponse, "The Modbus response is incomplete.");
        if (ModbusCrc16.Calculate(frame[..^2]) != (ushort)(frame[^2] | frame[^1] << 8)) throw new ModbusException(ModbusErrorKind.InvalidCrc, "The Modbus response CRC is invalid.");
        if (frame[0] != expectedSlaveAddress || frame[1] != expectedFunctionCode) throw new ModbusException(ModbusErrorKind.InvalidResponse, "The Modbus response address or function code does not match the request.");
        return new ModbusResponse(frame[0], frame[1], frame[2..^2].ToArray());
    }
}

public static class ModbusCrc16
{
    public static ushort Calculate(ReadOnlySpan<byte> data)
    { ushort crc = 0xFFFF; foreach (var value in data) { crc ^= value; for (var bit = 0; bit < 8; bit++) crc = (ushort)((crc & 1) != 0 ? (crc >> 1) ^ 0xA001 : crc >> 1); } return crc; }
}

public sealed record ModbusRegisterDefinition(ushort Address, byte FunctionCode, string DataType, ushort Length, decimal Scale, decimal Offset, string Unit, string Name, string Description, string Endianness);
