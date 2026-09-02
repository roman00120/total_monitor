using TotalMonitor.Core.Entities;

namespace TotalMonitor.Core.Interfaces;

public enum AcquisitionState { Stopped, Starting, Active, Faulted }

public sealed record AcquisitionEvent(string EventType, int? MeterId, string Message, DateTimeOffset Timestamp);

public sealed record AcquisitionStatusSummary(
    AcquisitionState State,
    MeterHardwareState HardwareState,
    string HardwareStatusText,
    string CurrentPort,
    int ActiveMetersCount,
    DateTimeOffset? LastAcquisitionTime,
    string? LastError,
    long TotalReadingsProcessed);

public interface IDataAcquisitionService : IAsyncDisposable
{
    AcquisitionState State { get; }
    MeterHardwareState HardwareState { get; }
    string CurrentPort { get; }
    int ActiveMetersCount { get; }
    DateTimeOffset? LastAcquisitionTime { get; }
    string? LastError { get; }
    long TotalReadingsProcessed { get; }
    IReadOnlyDictionary<int, Measurement> LastMeasurements { get; }
    IReadOnlyDictionary<int, MeterConnectionStatus> MeterStatuses { get; }
    event EventHandler<AcquisitionEvent>? EventRaised;
    event EventHandler<Measurement>? MeasurementReceived;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
    AcquisitionStatusSummary GetStatusSummary();
}
