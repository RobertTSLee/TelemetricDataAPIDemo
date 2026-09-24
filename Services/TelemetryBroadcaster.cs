using Microsoft.AspNetCore.SignalR;
using TelemetricDataAPIDemo.Hubs;

namespace TelemetricDataAPIDemo.Services;

public interface ITelemetryBroadcaster
{
    Task BroadcastBatchAsync(IEnumerable<TelemetryMetricDto> metrics);
    Task SendAlertToZoneAsync(string zoneId, TelemetryAlertDto alert);
}

public class TelemetryBroadcaster : ITelemetryBroadcaster
{
    private readonly IHubContext<TelemetryHub, ITelemetryClient> _hubContext;

    public TelemetryBroadcaster(IHubContext<TelemetryHub, ITelemetryClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastBatchAsync(IEnumerable<TelemetryMetricDto> metrics)
    {
        // Broadcasts to ALL connected Angular clients via Azure SignalR backplane
        await _hubContext.Clients.All.ReceiveTelemetryBatch(metrics);
    }

    public async Task SendAlertToZoneAsync(string zoneId, TelemetryAlertDto alert)
    {
        // Target specific subscribed group (Zone)
        await _hubContext.Clients.Group(zoneId).ReceiveCriticalAlert(alert);
    }
}
