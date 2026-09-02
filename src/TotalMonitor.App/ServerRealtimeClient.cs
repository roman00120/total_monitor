using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using TotalMonitor.Core.Interfaces;

namespace TotalMonitor.App;

public sealed record MeasurementUpdatedMessage(int MeterId, string Variable, decimal Value, string Unit, DateTimeOffset Timestamp);
public sealed record SignalRAcquisitionEvent(int? MeterId, string Message, DateTimeOffset Timestamp);

public sealed class ServerRealtimeClient(IApiClient api, IConfiguration configuration) : IAsyncDisposable
{
    private HubConnection? connection;
    public event EventHandler<MeasurementUpdatedMessage>? MeasurementUpdated;
    public event EventHandler<AcquisitionEvent>? AcquisitionEventReceived;
    public event EventHandler<string>? ConnectionStateChanged;

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (connection is not null && connection.State == HubConnectionState.Connected)
            return;

        var baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5080/";
        connection = new HubConnectionBuilder()
            .WithUrl(new Uri(new Uri(baseUrl), "hubs/monitoring"), options => options.AccessTokenProvider = () => Task.FromResult(api.Token))
            .WithAutomaticReconnect()
            .Build();

        connection.On<MeasurementUpdatedMessage>("MeasurementUpdated", message =>
        {
            MeasurementUpdated?.Invoke(this, message);
        });

        connection.On<SignalRAcquisitionEvent>("AcquisitionStarted", e =>
        {
            AcquisitionEventReceived?.Invoke(this, new AcquisitionEvent("AcquisitionStarted", e.MeterId, e.Message, e.Timestamp));
        });

        connection.On<SignalRAcquisitionEvent>("AcquisitionStopped", e =>
        {
            AcquisitionEventReceived?.Invoke(this, new AcquisitionEvent("AcquisitionStopped", e.MeterId, e.Message, e.Timestamp));
        });

        connection.On<SignalRAcquisitionEvent>("CycleCompleted", e =>
        {
            AcquisitionEventReceived?.Invoke(this, new AcquisitionEvent("CycleCompleted", e.MeterId, e.Message, e.Timestamp));
        });

        connection.On<SignalRAcquisitionEvent>("CycleError", e =>
        {
            AcquisitionEventReceived?.Invoke(this, new AcquisitionEvent("CycleError", e.MeterId, e.Message, e.Timestamp));
        });

        connection.On<SignalRAcquisitionEvent>("MeterStatusChanged", e =>
        {
            AcquisitionEventReceived?.Invoke(this, new AcquisitionEvent("MeterStatusChanged", e.MeterId, e.Message, e.Timestamp));
        });

        connection.Reconnecting += error =>
        {
            ConnectionStateChanged?.Invoke(this, "reconnecting");
            return Task.CompletedTask;
        };
        connection.Reconnected += _ =>
        {
            ConnectionStateChanged?.Invoke(this, "connected");
            return Task.CompletedTask;
        };
        connection.Closed += error =>
        {
            ConnectionStateChanged?.Invoke(this, "disconnected");
            return Task.CompletedTask;
        };

        try
        {
            await connection.StartAsync(ct);
            ConnectionStateChanged?.Invoke(this, "connected");
        }
        catch
        {
            ConnectionStateChanged?.Invoke(this, "disconnected");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
            connection = null;
        }
    }
}
