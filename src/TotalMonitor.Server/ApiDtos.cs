using TotalMonitor.Core.Historical;

namespace TotalMonitor.Server;

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResponse(string Token, int UserId, string Username, string DisplayName, string Role, IReadOnlySet<string> Permissions, DateTimeOffset ExpiresAt);
public sealed record UserDto(int Id, string Username, string DisplayName, bool IsActive, DateTimeOffset? LastLoginAt);
public sealed record MeterDto(int Id, string Name, string Model, byte ModbusAddress, string ComPort, int BaudRate, string Parity, bool IsEnabled);
public sealed record MeterStatusDto(int MeterId, string State, DateTimeOffset? LastSuccessfulCommunication, string? LastError, int ConsecutiveFailures, int? LastResponseTimeMilliseconds);
public sealed record MeasurementDto(long Id, int MeterId, string MeterName, DateTimeOffset Timestamp, string Variable, decimal Value, string Unit);
public sealed record HistoricalQueryDto(DateTimeOffset From, DateTimeOffset To, int? MeterId, string? Variable, HistoricalResolution Resolution = HistoricalResolution.Automatic, int PageNumber = 1, int PageSize = 500);
public sealed record ReportRequestDto(DateTimeOffset From, DateTimeOffset To, int? MeterId, string? Variable, HistoricalResolution Resolution = HistoricalResolution.Automatic);
public sealed record ReportResponseDto(int TotalCount, DateTimeOffset GeneratedAt, IReadOnlyList<MeasurementDto> Items);
public sealed record ApiErrorResponse(string Code, string Message);

public sealed record CommunicationSettingsDto(
    int Id,
    string ComPort,
    int BaudRate,
    int DataBits,
    string Parity,
    string StopBits,
    int ReadTimeout,
    int WriteTimeout,
    int PollingInterval,
    DateTimeOffset UpdatedAt);

public sealed record CommunicationSettingsRequest(
    string ComPort,
    int BaudRate,
    int DataBits,
    string Parity,
    string StopBits,
    int ReadTimeout,
    int WriteTimeout,
    int PollingInterval);

public sealed record TestConnectionRequest(
    string? ComPort,
    byte SlaveAddress,
    int BaudRate,
    string? Parity,
    int DataBits,
    string? StopBits,
    int Timeout);

public sealed record TestConnectionResponse(bool Success, string Message, string HardwareState);

public sealed record AcquisitionStatusDto(
    string State,
    string HardwareState,
    string HardwareStatusText,
    string CurrentPort,
    int ActiveMetersCount,
    DateTimeOffset? LastAcquisitionTime,
    string? LastError,
    long TotalReadingsProcessed);
