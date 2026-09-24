using Microsoft.EntityFrameworkCore;
using TelemetryIngestor.Models;

namespace TelemetryIngestor.Repositories;

public class TelemetryDbContext : DbContext
{
    public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options)
    {
    }

    public DbSet<TelemetryMetric> TelemetryMetrics => Set<TelemetryMetric>();

    // defintion of the entity model and its properties
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TelemetryMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceId).IsRequired();
            entity.Property(e => e.MetricName).IsRequired();
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.TimeStamp).IsRequired();
            entity.Property(e => e.IngestAt).IsRequired();

            entity.HasIndex(e => new { e.DeviceId, e.TimeStamp });
        });
    }
}
