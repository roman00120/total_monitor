using TotalMonitor.Core.Modbus;

namespace TotalMonitor.Core.Tests;

public sealed class MeasurementCatalogTests
{
    [Fact]
    public void Catalog_contains_required_measurement_families_without_register_addresses()
    {
        var keys = Tov452MeasurementCatalog.Expected.Select(x => x.Key).ToHashSet();

        Assert.Subset(
            new HashSet<string>
            {
                "V1", "V2", "V3", "I1", "I2", "I3", "P1", "P2", "P3",
                "Voltage", "Current", "ActivePower", "ReactivePower",
                "ApparentPower", "PowerFactor", "Frequency", "Energy",
                "Demand", "VoltageThd", "CurrentThd", "PF", "THD"
            },
            keys);
    }

    [Fact]
    public void Tov452_register_map_is_empty_until_official_map_is_supplied()
    {
        Assert.Empty(TOV452RegisterMap.Empty.Entries);
        Assert.False(TOV452RegisterMap.Empty.IsConfigured);
    }
}
