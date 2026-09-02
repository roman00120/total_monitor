using TotalMonitor.Core.Entities;

namespace TotalMonitor.Core.Modbus;

/// <summary>
/// Simulador explícito para pruebas del TOV452 sin hardware físico.
/// Genera lecturas eléctricas coherentes etiquetadas como simulación.
/// </summary>
public sealed class MockTov452Generator
{
    private readonly Random random = new(42);
    private decimal accumulatedEnergy = 1250.50m;

    public IReadOnlyList<Measurement> GenerateMeasurements(Meter meter, DateTimeOffset timestamp)
    {
        var meterId = meter.Id > 0 ? meter.Id : 1;

        var v1 = (decimal)(220.0 + (random.NextDouble() * 4.0 - 2.0));
        var v2 = (decimal)(221.0 + (random.NextDouble() * 4.0 - 2.0));
        var v3 = (decimal)(219.5 + (random.NextDouble() * 4.0 - 2.0));
        var vAvg = Math.Round((v1 + v2 + v3) / 3m, 2);

        var i1 = (decimal)(8.5 + (random.NextDouble() * 1.5 - 0.75));
        var i2 = (decimal)(9.1 + (random.NextDouble() * 1.5 - 0.75));
        var i3 = (decimal)(8.8 + (random.NextDouble() * 1.5 - 0.75));
        var iAvg = Math.Round((i1 + i2 + i3) / 3m, 2);

        var pf = (decimal)Math.Round(0.95 + random.NextDouble() * 0.03, 3);
        var freq = (decimal)Math.Round(59.95 + random.NextDouble() * 0.1, 2);

        var p1 = Math.Round(v1 * i1 * pf, 2);
        var p2 = Math.Round(v2 * i2 * pf, 2);
        var p3 = Math.Round(v3 * i3 * pf, 2);
        var pTotal = Math.Round(p1 + p2 + p3, 2);

        var qTotal = (decimal)Math.Round((double)pTotal * Math.Tan(Math.Acos((double)pf)), 2);
        var sTotal = Math.Round(pTotal / pf, 2);

        accumulatedEnergy += Math.Round(pTotal / 3600m / 1000m, 4);

        var vThd = (decimal)Math.Round(1.5 + random.NextDouble() * 0.8, 2);
        var iThd = (decimal)Math.Round(2.2 + random.NextDouble() * 1.1, 2);

        return
        [
            new(meterId, meter.Name, timestamp, "V1", Math.Round(v1, 2), "V"),
            new(meterId, meter.Name, timestamp, "V2", Math.Round(v2, 2), "V"),
            new(meterId, meter.Name, timestamp, "V3", Math.Round(v3, 2), "V"),
            new(meterId, meter.Name, timestamp, "Voltage", vAvg, "V"),
            new(meterId, meter.Name, timestamp, "I1", Math.Round(i1, 2), "A"),
            new(meterId, meter.Name, timestamp, "I2", Math.Round(i2, 2), "A"),
            new(meterId, meter.Name, timestamp, "I3", Math.Round(i3, 2), "A"),
            new(meterId, meter.Name, timestamp, "Current", iAvg, "A"),
            new(meterId, meter.Name, timestamp, "P1", p1, "W"),
            new(meterId, meter.Name, timestamp, "P2", p2, "W"),
            new(meterId, meter.Name, timestamp, "P3", p3, "W"),
            new(meterId, meter.Name, timestamp, "ActivePower", pTotal, "W"),
            new(meterId, meter.Name, timestamp, "ReactivePower", qTotal, "var"),
            new(meterId, meter.Name, timestamp, "ApparentPower", sTotal, "VA"),
            new(meterId, meter.Name, timestamp, "PF", pf, ""),
            new(meterId, meter.Name, timestamp, "PowerFactor", pf, ""),
            new(meterId, meter.Name, timestamp, "Frequency", freq, "Hz"),
            new(meterId, meter.Name, timestamp, "Energy", Math.Round(accumulatedEnergy, 2), "kWh"),
            new(meterId, meter.Name, timestamp, "Demand", pTotal, "W"),
            new(meterId, meter.Name, timestamp, "VoltageThd", vThd, "%"),
            new(meterId, meter.Name, timestamp, "CurrentThd", iThd, "%"),
            new(meterId, meter.Name, timestamp, "THD", vThd, "%")
        ];
    }
}
