using Microsoft.AspNetCore.SignalR;
using TelemetryIngestor.Models;

namespace TelemetricDataAPIDemo.Hubs;


public record TelemetryMetricDto(string DeviceId, string MetricName, double Value, DateTime Timestamp);
public record TelemetryAlertDto(string DeviceId, string Message, double Value, DateTime Timestamp);

public interface ITelemetryClient
{
    Task ReceiveTelemetryBatch(IEnumerable<TelemetryMetricDto> metrics);
    Task ReceiveCriticalAlert(TelemetryAlertDto alert);
}

/*
 * Connects clients to the telemetry hub and allows them to join or leave groups for receiving telemetry data.
 * Track clients by their connection IDs and group memberships to manage real-time data delivery.
 */
public class TelemetryHub : Hub<ITelemetryClient>
{
    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
    }

    public async Task LeaveGroup(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        // Optionally, you can send a welcome message or perform other actions here
    }
}
