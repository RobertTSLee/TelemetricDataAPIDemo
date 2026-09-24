using TelemetryIngestor.Models;
using TelemetryIngestor.Repositories;
using TelemetricDataAPIDemo.Hubs;
using TelemetricDataAPIDemo.Services;
using Microsoft.AspNetCore.SignalR;

namespace TelemetryIngestor.Services;

/*
 * This server reads telemetry metrics from the in-memory channel 
 * and persists them to the database.
 * Defined as a background service that runs continuously in the background,
 * Batch the metrics and persist them to the database in a single transaction 
 * to improve performance.
 * Also monitors for critical temperature alerts and broadcasts them via SignalR.
 */

public class TelementryIngestorWorker : BackgroundService
{
    private readonly ITelementryChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelementryIngestorWorker> _logger;
    private readonly IHubContext<TelemetryHub, ITelemetryClient> _hubContext;

    private const int BatchSize = 500; // Number of metrics to batch before persisting to the database
    private static readonly TimeSpan BatchInterval = TimeSpan.FromSeconds(3); // Interval to wait before persisting metrics to the database
    private const string TemperatureMetricName = "temperature";
    private const double TemperatureAlertThreshold = 90.0;

    public TelementryIngestorWorker(
        ITelementryChannel channel, 
        IServiceScopeFactory scopeFactory, 
        ILogger<TelementryIngestorWorker> logger,
        IHubContext<TelemetryHub, ITelemetryClient> hubContext)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buffer = new List<TelemetryMetric>(BatchSize);
        var lastFlushTime = DateTime.UtcNow;

        try
        {
            var readTask = ReadFromChannelAsync(buffer, stoppingToken);
            var flushTask = FlushBufferAsync(buffer, stoppingToken);

            await Task.WhenAny(readTask, flushTask);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation($"Telemetry ingestor worker is stopping: {ex.Message}");

            // Flush any remaining items in the buffer before shutting down
            if (buffer.Count > 0)
            {
                await PersistMetricsAsync(buffer, stoppingToken);
            }
        }
        catch (Exception ex)
        {            
            _logger.LogError($"Error occurred while ingesting telemetry metrics: {ex.Message}");            
        }
    }

    private async Task ReadFromChannelAsync(List<TelemetryMetric> buffer, CancellationToken stoppingToken)
    {
        await foreach (var item in _channel.ReadAllAsync(stoppingToken))
        {
            buffer.Add(item);

            // Broadcast metric to clients immediately (real-time delivery)
            await BroadcastMetricAsync(item, stoppingToken);

            // Check for critical temperature alert
            await CheckAndBroadcastTemperatureAlertAsync(item, stoppingToken);

            // buffer is full, persist the metrics to the database
            if (buffer.Count >= BatchSize)
            {
                await PersistMetricsAsync(buffer, stoppingToken);
                buffer.Clear();
            }
        }
    }

    private async Task FlushBufferAsync(List<TelemetryMetric> buffer, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(BatchInterval);

        try
        {
            // Flush the buffer at regular intervals
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (buffer.Count > 0)
                {
                    await PersistMetricsAsync(buffer, stoppingToken);
                    buffer.Clear();
                }
            }
        }
        finally
        {
            timer.Dispose();
        }
    }

    private async Task PersistMetricsAsync(List<TelemetryMetric> metrics, CancellationToken cancellationToken)
    {
        if (metrics.Count == 0)
            return;

        try
        {
            using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

            foreach (var metric in metrics)
            {
                metric.IngestAt = DateTime.UtcNow;
            }

            await dbContext.TelemetryMetrics.AddRangeAsync(metrics, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Persisted {metrics.Count} telemetry metrics to the database.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error persisting telemetry metrics to the database: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Broadcasts a single telemetry metric to all connected clients via SignalR.
    /// This enables real-time delivery before the metric is persisted to the database.
    /// </summary>
    private async Task BroadcastMetricAsync(TelemetryMetric metric, CancellationToken cancellationToken)
    {
        try
        {
            // Convert to DTO for client consumption
            var metricDto = new TelemetryMetricDto(
                DeviceId: metric.DeviceId,
                MetricName: metric.MetricName,
                Value: metric.Value,
                Timestamp: metric.TimeStamp
            );

            // Broadcast to all connected clients
            await _hubContext.Clients.All.ReceiveTelemetryBatch(new[] { metricDto });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error broadcasting telemetry metric: {ex.Message}");
            // Don't throw - we don't want broadcast failures to block metric ingestion
        }
    }

    /// <summary>
    /// Checks if a metric is a temperature reading that exceeds the alert threshold.
    /// If so, broadcasts a critical alert via SignalR to all connected clients.
    /// </summary>
    private async Task CheckAndBroadcastTemperatureAlertAsync(TelemetryMetric metric, CancellationToken cancellationToken)
    {
        try
        {
            // Check if this is a temperature metric and if it exceeds the threshold
            if (metric.MetricName.Equals(TemperatureMetricName, StringComparison.OrdinalIgnoreCase) 
                && metric.Value > TemperatureAlertThreshold)
            {
                var alert = new TelemetryAlertDto(
                    DeviceId: metric.DeviceId,
                    Message: $"CRITICAL: Temperature alert for device {metric.DeviceId}. Current temperature: {metric.Value}°C (Threshold: {TemperatureAlertThreshold}°C)",
                    Value: metric.Value,
                    Timestamp: metric.TimeStamp
                );

                // Broadcast alert to all connected clients
                await _hubContext.Clients.All.ReceiveCriticalAlert(alert);

                _logger.LogWarning($"TEMPERATURE ALERT: Device {metric.DeviceId} temperature {metric.Value}°C exceeds threshold {TemperatureAlertThreshold}°C");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error broadcasting temperature alert: {ex.Message}");
            // Don't throw - we don't want alert processing to block metric ingestion
        }
    }
}
