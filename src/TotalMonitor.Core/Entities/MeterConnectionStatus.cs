namespace TotalMonitor.Core.Entities;

public sealed class MeterConnectionStatus
{
    public int Id { get; private set; }
    public int MeterId { get; private set; }
    public bool IsConnected { get; private set; }
    public string State { get; private set; } = "Disconnected";
    public DateTimeOffset? LastCheckedAt { get; private set; }
    public DateTimeOffset? LastSuccessfulCommunication { get; private set; }
    public string? LastError { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public int? LastResponseTimeMilliseconds { get; private set; }
    private MeterConnectionStatus() { }
    public MeterConnectionStatus(int meterId, bool isConnected, DateTimeOffset? lastCheckedAt = null)
    { MeterId = meterId; IsConnected = isConnected; LastCheckedAt = lastCheckedAt; }
    public void MarkSuccess(DateTimeOffset at, int responseTimeMilliseconds) { IsConnected = true; State = "Connected"; LastCheckedAt = at; LastSuccessfulCommunication = at; LastError = null; ConsecutiveFailures = 0; LastResponseTimeMilliseconds = responseTimeMilliseconds; }
    public void MarkFailure(DateTimeOffset at, string error) { IsConnected = false; State = error.Contains("timeout", StringComparison.OrdinalIgnoreCase) ? "Timeout" : "CommunicationError"; LastCheckedAt = at; LastError = error; ConsecutiveFailures++; }
}
