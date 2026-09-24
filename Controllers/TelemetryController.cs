using Microsoft.AspNetCore.Mvc;
using TelemetryIngestor.Models;
using TelemetryIngestor.Services;

namespace TelemetricDataAPIDemo.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TelemetryController(ITelementryChannel channel) : ControllerBase
{
    [HttpPost]
    public IActionResult Ingest([FromBody] TelemetryIngestDto dto)
    {
        var metric = new TelemetryMetric
        {
            DeviceId = dto.DeviceId,
            MetricName = dto.MetricName,
            Value = dto.Value,
            TimeStamp = dto.TimeStamp,
            IngestAt = DateTime.UtcNow
        };

        // Try to write the metric to the channel
        if (!channel.TryWrite(metric))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, "Telemetry channel is full. Please try again later.");
        }

        // Return a 202 Accepted response indicating that the data has been accepted for processing
        return Accepted(new { Message = "Telemetry data ingested successfully." });
    }
}
