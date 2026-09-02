using TotalMonitor.Core.Entities;

namespace TotalMonitor.Core.Modbus;

public sealed record RegisterGroup(string Name, byte FunctionCode, ushort Address, ushort Quantity, IReadOnlyList<ModbusRegisterDefinition> Definitions);

public sealed class MeterRegisterMap
{
    public IReadOnlyList<RegisterGroup> Groups { get; }

    public MeterRegisterMap(IEnumerable<RegisterGroup>? groups = null)
    {
        Groups = (groups ?? []).ToArray();
    }

    public static MeterRegisterMap Empty { get; } = new();

    public IReadOnlyList<Measurement> ParseResponse(Meter meter, RegisterGroup group, ReadOnlySpan<byte> data, DateTimeOffset timestamp)
    {
        var results = new List<Measurement>();
        if (data.Length < 3) return results;

        var byteCount = data[0];
        var payload = data.Slice(1);

        foreach (var def in group.Definitions)
        {
            var offset = (def.Address - group.Address) * 2;
            if (offset < 0 || offset + (def.Length * 2) > payload.Length)
                continue;

            decimal val = 0;
            if (def.Length == 1)
            {
                var raw = (short)((payload[offset] << 8) | payload[offset + 1]);
                val = ((decimal)raw * def.Scale) + def.Offset;
            }
            else if (def.Length == 2)
            {
                var raw = (payload[offset] << 24) | (payload[offset + 1] << 16) | (payload[offset + 2] << 8) | payload[offset + 3];
                val = ((decimal)raw * def.Scale) + def.Offset;
            }

            results.Add(new Measurement(meter.Id, meter.Name, timestamp, def.Name, val, def.Unit));
        }

        return results;
    }
}
