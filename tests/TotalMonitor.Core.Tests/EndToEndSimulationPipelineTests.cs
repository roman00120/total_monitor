using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Historical;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Core.Modbus;
using TotalMonitor.Core.Security;
using TotalMonitor.Infrastructure.Acquisition;
using TotalMonitor.Infrastructure.Historical;
using TotalMonitor.Infrastructure.Modbus;
using TotalMonitor.Infrastructure.Services;

namespace TotalMonitor.Core.Tests;

public sealed class EndToEndSimulationPipelineTests
{
    [Fact]
    public async Task EndToEnd_Complete_Hardware_Workflow_Test()
    {
        // 1 & 2 & 3: Configure Services & In-Memory Repositories
        var services = new ServiceCollection();
        services.AddLogging();
        var storage = new InMemoryStorageContext();

        services.AddScoped<ICommunicationSettingsService>(_ => storage);
        services.AddScoped<IMeterService>(_ => storage);
        services.AddScoped<IMeasurementRepository>(_ => storage);
        services.AddScoped<IMeterConnectionStatusRepository>(_ => storage);
        services.AddScoped<IUnitOfWork>(_ => storage);
        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();
        services.AddScoped<IAuthorizationService, FakeAuthorizationService>();
        services.AddScoped<IAuditService, FakeAuditService>();
        services.AddScoped<IHistoricalDataService, HistoricalDataService>();
        services.AddScoped<IDataAggregationService, DataAggregationService>();
        services.AddSingleton<IHistoricalDataExporter, CsvHistoricalDataExporter>();
        services.AddScoped<IReportService, ReportService>();

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        // 4. Authenticate & Configure Admin Settings
        var settingsService = sp.GetRequiredService<ICommunicationSettingsService>();
        var settings = await settingsService.GetSettingsAsync();
        Assert.NotNull(settings);

        // 5. Create a TOV452 Meter
        var meterService = sp.GetRequiredService<IMeterService>();
        var meter = new Meter("TOV452 Principal", 1, "COM1", 9600, "None", true, "TOV452");
        typeof(Meter).GetProperty("Id")?.SetValue(meter, 101);
        await meterService.CreateAsync(meter);

        var allMeters = await meterService.GetAllAsync();
        Assert.Single(allMeters);
        Assert.Equal("TOV452", allMeters[0].Model);

        // 6. Configure hardware communication
        settings.Update("COM1", 9600, 8, "None", "One", 1000, 1000, 50);
        await settingsService.SaveSettingsAsync(settings);

        // 7. Start Acquisition Engine with Register Map definitions
        var options = new ModbusConnectionOptions { ComPort = "COM1" };
        var clients = new FakeClientFactory();

        var registerGroup = new RegisterGroup("StandardVariables", 0x03, 0x0000, 6, [
            new ModbusRegisterDefinition(0x0000, 0x03, "Int16", 1, 0.1m, 0m, "V", "Voltage", "Voltaje", "BigEndian"),
            new ModbusRegisterDefinition(0x0001, 0x03, "Int16", 1, 0.01m, 0m, "A", "Current", "Corriente", "BigEndian"),
            new ModbusRegisterDefinition(0x0002, 0x03, "Int16", 1, 1.0m, 0m, "W", "ActivePower", "Potencia Activa", "BigEndian"),
            new ModbusRegisterDefinition(0x0003, 0x03, "Int16", 1, 0.1m, 0m, "Hz", "Frequency", "Frecuencia", "BigEndian"),
            new ModbusRegisterDefinition(0x0004, 0x03, "Int32", 2, 0.1m, 0m, "kWh", "Energy", "Energia", "BigEndian")
        ]);
        var registerMap = new MeterRegisterMap([registerGroup]);

        var acq = new DataAcquisitionService(
            scopeFactory,
            clients,
            registerMap,
            options,
            NullLogger<DataAcquisitionService>.Instance);

        var receivedReadings = new List<Measurement>();
        acq.MeasurementReceived += (_, m) => receivedReadings.Add(m);

        await acq.StartAsync();
        Assert.True(acq.State is AcquisitionState.Active or AcquisitionState.Starting);

        // 8 & 9: Process real Modbus readings and persist in storage
        await Task.Delay(350); // Allow multiple polling cycles to process

        // 10: Verify status via summary / API model
        var statusSummary = acq.GetStatusSummary();
        Assert.Equal(AcquisitionState.Active, statusSummary.State);
        Assert.Equal(MeterHardwareState.ADQUIRIENDO, statusSummary.HardwareState);
        Assert.True(acq.TotalReadingsProcessed > 0);
        Assert.NotEmpty(storage.Measurements);

        // 11: Verify Dashboard data points
        Assert.Contains(storage.Measurements, m => m.Variable == "Voltage" && m.Value > 200);
        Assert.Contains(storage.Measurements, m => m.Variable == "ActivePower" && m.Value > 0);
        Assert.Contains(storage.Measurements, m => m.Variable == "Energy" && m.Value > 0);

        // 12: Verify Real-time Monitoring telemetry reception
        Assert.NotEmpty(receivedReadings);
        Assert.Contains(receivedReadings, m => m.Variable == "Frequency" && m.Value >= 50.0m);

        // 13: Query Historical Data
        var historyService = sp.GetRequiredService<IHistoricalDataService>();
        var historyFilter = new HistoricalQueryFilter(
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddHours(1),
            MeterId: 101,
            PageNumber: 1,
            PageSize: 100);

        var historyPage = await historyService.QueryAsync(historyFilter);
        Assert.True(historyPage.TotalCount > 0);
        Assert.NotEmpty(historyPage.Items);

        // 14: Generate Report from persisted history
        var reportService = sp.GetRequiredService<IReportService>();
        var reportRequest = new ReportRequest(
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddHours(1));

        var reportResult = await reportService.GenerateAsync(reportRequest);
        Assert.NotNull(reportResult);
        Assert.True(reportResult.Data.TotalCount > 0);

        // 15: Stop Acquisition Engine cleanly
        await acq.StopAsync();
        Assert.Equal(AcquisitionState.Stopped, acq.State);

        // 16: Confirm clean shutdown and final state
        var finalSummary = acq.GetStatusSummary();
        Assert.Equal(AcquisitionState.Stopped, finalSummary.State);
    }

    private sealed class FakeAuditService : IAuditService
    {
        public Task RecordAsync(string action, string description, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public AuthenticatedUser? User { get; private set; } = new(1, "admin", "Admin", "Admin", new HashSet<string> { "Reports.View", "Reports.Export" });
        public bool IsAuthenticated => true;
        public void SetUser(AuthenticatedUser user) => User = user;
        public void Clear() => User = null;
    }

    private sealed class FakeAuthorizationService : IAuthorizationService
    {
        public bool HasPermission(string permission) => true;
        public bool HasAnyPermission(params string[] permissions) => true;
        public bool HasAllPermissions(params string[] permissions) => true;
        public void Demand(string permission) { }
    }

    private sealed class InMemoryStorageContext
        : ICommunicationSettingsService, IMeterService, IMeasurementRepository, IMeterConnectionStatusRepository, IUnitOfWork
    {
        public List<Measurement> Measurements { get; } = [];
        private readonly List<Meter> meters = [];
        private CommunicationSettings settings = new("COM1", 9600, 8, "None", "One", 1000, 1000, 50);
        private readonly Dictionary<int, MeterConnectionStatus> statuses = [];

        public Task<CommunicationSettings> GetSettingsAsync(CancellationToken ct = default) => Task.FromResult(settings);
        public Task<CommunicationSettings> SaveSettingsAsync(CommunicationSettings s, CancellationToken ct = default)
        {
            settings = s;
            return Task.FromResult(settings);
        }

        public Task<IReadOnlyList<string>> GetAvailablePortsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(["COM1", "COM3"]);
        public Task<(bool Success, string Message, MeterHardwareState State)> TestConnectionAsync(
            string? comPort, byte slaveAddress, int baudRate, string? parity, int dataBits, string? stopBits, int timeout, CancellationToken ct = default) =>
            Task.FromResult((true, "OK", MeterHardwareState.CONECTADO));

        public Task<IReadOnlyList<Meter>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Meter>>(meters);
        public Task<Meter> CreateAsync(Meter m, CancellationToken ct = default)
        {
            meters.Add(m);
            return Task.FromResult(m);
        }
        public Task<bool> UpdateAsync(int id, string name, byte address, string comPort, int baudRate, string parity, bool enabled, string model = "TOV452", CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            meters.RemoveAll(m => m.Id == id);
            return Task.FromResult(true);
        }

        public Task AddRangeAsync(IEnumerable<Measurement> items, CancellationToken ct = default)
        {
            Measurements.AddRange(items);
            return Task.CompletedTask;
        }

        public Task<HistoricalPage> GetRangeAsync(HistoricalQueryFilter filter, CancellationToken cancellationToken = default)
        {
            var query = Measurements.AsEnumerable();
            if (filter.MeterId.HasValue) query = query.Where(x => x.MeterId == filter.MeterId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Variable)) query = query.Where(x => x.Variable == filter.Variable);

            var list = query.Select(m => new HistoricalDataPoint(m.Id, m.MeterId, "TOV452 Principal", m.Timestamp, m.Variable, m.Value, m.Unit)).ToList();
            return Task.FromResult(new HistoricalPage(list, list.Count, filter.PageNumber, filter.PageSize));
        }

        public Task<IReadOnlyList<HistoricalDataPoint>> GetLatestAsync(int meterId, string? variable = null, int take = 100, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HistoricalDataPoint>>([]);

        public Task<IReadOnlyList<HistoricalDataPoint>> GetByMeterAsync(int meterId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HistoricalDataPoint>>([]);

        public Task<IReadOnlyList<HistoricalDataPoint>> GetByVariableAsync(string variable, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HistoricalDataPoint>>([]);

        public Task<IReadOnlyList<string>> GetVariablesAsync(int? meterId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(["Voltage", "Current", "ActivePower", "Frequency", "PowerFactor", "Energy"]);

        public Task<MeterConnectionStatus> GetOrCreateAsync(int meterId, CancellationToken ct = default)
        {
            if (!statuses.TryGetValue(meterId, out var status))
            {
                status = new MeterConnectionStatus(meterId, true, DateTimeOffset.UtcNow);
                statuses[meterId] = status;
            }
            return Task.FromResult(status);
        }

        public Task SaveAsync(MeterConnectionStatus status, CancellationToken ct = default)
        {
            statuses[status.MeterId] = status;
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(Measurements.Count);
    }

    private sealed class FakeClientFactory : IModbusClientFactory
    {
        public IModbusClient Create(Meter meter) => new FakeModbusClient();
    }

    private sealed class FakeModbusClient : IModbusClient
    {
        public Task<ModbusResponse> SendAsync(ModbusRequest request, CancellationToken ct = default)
        {
            // Return 12 bytes representing registers:
            // Reg 0: Voltage = 2200 (220.0 V)
            // Reg 1: Current = 500 (5.00 A)
            // Reg 2: ActivePower = 1100 (1100 W)
            // Reg 3: Frequency = 600 (60.0 Hz)
            // Reg 4-5: Energy = 1500 (150.0 kWh)
            var responseData = new byte[] {
                12,
                0x08, 0x98, // 2200
                0x01, 0xF4, // 500
                0x04, 0x4C, // 1100
                0x02, 0x58, // 600
                0x00, 0x00, 0x05, 0xDC // 1500
            };
            return Task.FromResult(new ModbusResponse(request.SlaveAddress, request.FunctionCode, responseData));
        }
    }
}
