using TotalMonitor.Core.Entities;

namespace TotalMonitor.Core.Tests;
public sealed class DomainTests
{
    [Fact] public void Meter_requires_valid_address() => Assert.Throws<ArgumentOutOfRangeException>(() => new Meter("M1", 0, "COM1", 9600, "None"));
    [Fact] public void Meter_trims_name() => Assert.Equal("M1", new Meter(" M1 ", 1, "COM1", 9600, "None").Name);
    [Fact] public void Measurement_requires_meter() => Assert.Throws<ArgumentOutOfRangeException>(() => new Measurement(0, DateTimeOffset.UtcNow, "Documented variable", 1, "unit"));
    [Fact] public void Measurement_preserves_documented_variable_name() => Assert.Equal("Documented variable", new Measurement(1, DateTimeOffset.UtcNow, "Documented variable", 1, "unit").Variable);
}
