using Microsoft.AspNetCore.SignalR;
using TotalMonitor.Core.Entities;
using TotalMonitor.Core.Interfaces;
using TotalMonitor.Server.Hubs;

namespace TotalMonitor.Server;

public sealed class AcquisitionHostedService(
    IDataAcquisitionService acquisition,
    IHubContext<MonitoringHub> hub,
    IConfiguration configuration,
    ILogger<AcquisitionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        acquisition.EventRaised += OnEvent;
        acquisition.MeasurementReceived += OnMeasurement;

        if (bool.TryParse(configuration["Acquisition:Enabled"], out var enabled) && enabled)
        {
            logger.LogInformation("Central acquisition auto-start is enabled in configuration.");
            try
            {
                await acquisition.StartAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to auto-start central acquisition.");
            }
        }
        else
        {
            logger.LogInformation("Central acquisition is ready (manual start via API/UI).");
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            acquisition.EventRaised -= OnEvent;
            acquisition.MeasurementReceived -= OnMeasurement;
            await acquisition.StopAsync();
        }
    }

    private void OnEvent(object? sender, AcquisitionEvent e)
    {
        _ = PublishEventAsync(e);
    }

    private void OnMeasurement(object? sender, Measurement m)
    {
        _ = PublishMeasurementAsync(m);
    }

    private async Task PublishEventAsync(AcquisitionEvent e)
    {
        try
        {
            await hub.Clients.All.SendAsync(e.EventType, new { e.MeterId, e.Message, e.Timestamp });
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unable to publish acquisition event to SignalR.");
        }
    }

    private async Task PublishMeasurementAsync(Measurement m)
    {
        try
        {
            await hub.Clients.All.SendAsync("MeasurementUpdated", new
            {
                MeterId = m.MeterId,
                Variable = m.Variable,
                Value = m.Value,
                Unit = m.Unit,
                Timestamp = m.Timestamp
            });
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unable to publish measurement to SignalR.");
        }
    }
}
