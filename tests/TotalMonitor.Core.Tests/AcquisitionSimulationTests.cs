using TotalMonitor.Core.Entities;
using TotalMonitor.Infrastructure.Modbus;

namespace TotalMonitor.Core.Tests;
public sealed class AcquisitionSimulationTests
{
    [Fact]
    public async Task Simulator_records_requests_without_claiming_real_connection()
    {
        await using var transport = new MockModbusTransport(request => request);
        await transport.OpenAsync();
        var request = new byte[] { 1, 3, 0, 0, 0, 1, 0, 0 };
        await transport.ExchangeAsync(request);
        Assert.True(transport.IsOpen);
        Assert.Single(transport.Requests);
        Assert.Equal(request, transport.Requests[0]);
    }

    [Fact]
    public void Load_shape_supports_250_configured_meters_as_a_collection()
    {
        var meters = Enumerable.Range(1, 250).Select(id => new Meter($"SIMULATOR-{id}", (byte)((id - 1) % 247 + 1), "COM-SIMULATOR", 9600, "None")).ToArray();
        Assert.Equal(250, meters.Length);
        Assert.Equal(250, meters.Select(m => m.Name).Distinct().Count());
    }
}
