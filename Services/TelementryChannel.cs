using System.Threading.Channels;
using TelemetryIngestor.Models;

namespace TelemetryIngestor.Services;

public interface ITelementryChannel
{
    bool TryWrite(TelemetryMetric metric);
    IAsyncEnumerable<TelemetryMetric> ReadAllAsync(CancellationToken token);
}

/*
 * In memory channel to store telemetry metrics before they are persisted to the database. 
 * This allows for asynchronous processing 
 * and can help to improve performance and scalability of the application.
 */
public class TelementryChannel: ITelementryChannel
{
    private readonly Channel<TelemetryMetric> _channel;

    public TelementryChannel(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            SingleWriter = false,  // Multiple API threads pushing data concurrently
            SingleReader = true,   // Single BackgroundService consuming the queue
            FullMode = BoundedChannelFullMode.Wait // Applies backpressure when buffer reaches capacity
        };
        _channel = Channel.CreateBounded<TelemetryMetric>(options);
    }

    public bool TryWrite(TelemetryMetric metric)
    {
        return _channel.Writer.TryWrite(metric);
    }

    public IAsyncEnumerable<TelemetryMetric> ReadAllAsync(CancellationToken token)
    {
        return _channel.Reader.ReadAllAsync(token);
    }
}
