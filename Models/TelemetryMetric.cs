namespace TelemetryIngestor.Models;
public class TelemetryMetric
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime TimeStamp { get; set; }
    public DateTime IngestAt { get; set; }
}

public record TelemetryIngestDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime TimeStamp { get; set; }
}