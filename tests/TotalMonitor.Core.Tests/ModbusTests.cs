using TotalMonitor.Core.Modbus;

namespace TotalMonitor.Core.Tests;
public sealed class ModbusTests
{
    [Fact] public void Crc_matches_modbus_reference_frame() => Assert.Equal((ushort)0xCDC5, ModbusCrc16.Calculate([0x01, 0x03, 0x00, 0x00, 0x00, 0x0A]));
    [Fact] public void Request_appends_crc_in_low_byte_first_order() => Assert.Equal([0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD], new ModbusRequest(1, 3, [0, 0, 0, 10]).ToFrame());
    [Fact] public void Request_rejects_invalid_slave_address() => Assert.Throws<ArgumentOutOfRangeException>(() => new ModbusRequest(0, 3, []));
    [Fact] public void Response_rejects_invalid_crc()
    { var frame = new byte[] { 1, 3, 2, 0, 1, 0, 0 }; Assert.Throws<ModbusException>(() => ModbusResponse.Parse(frame, 1, 3)); }
    [Fact] public void Response_validates_and_exposes_data()
    { var body = new byte[] { 1, 3, 2, 0, 1 }; var crc = ModbusCrc16.Calculate(body); var frame = body.Concat([(byte)crc, (byte)(crc >> 8)]).ToArray(); Assert.Equal([2, 0, 1], ModbusResponse.Parse(frame, 1, 3).Data); }
}
