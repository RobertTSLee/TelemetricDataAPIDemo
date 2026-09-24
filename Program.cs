using Microsoft.EntityFrameworkCore;
using TelemetricDataAPIDemo.Hubs;
using TelemetryIngestor.Models;
using TelemetryIngestor.Repositories;
using TelemetryIngestor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add Swagger/OpenAPI UI
builder.Services.AddSwaggerGen();

// Add SignalR services
builder.Services.AddSignalR();

// Register DbContext with PostgreSQL provider
builder.Services.AddDbContext<TelemetryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());

// Register the telemetry channel as a singleton
builder.Services.AddSingleton<ITelementryChannel>(new TelementryChannel(capacity: 10000));

// Register the background service that will process telemetry metrics
builder.Services.AddHostedService<TelementryIngestorWorker>();

// Add CORS policy for cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost", "https://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR requires credentials for cross-origin WebSockets
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Telemetry Data API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at the root
        options.DefaultModelsExpandDepth(2);
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Apply CORS policy - MUST be before MapHub
app.UseCors("AllowAngularClient");

// Map Hub Endpoint
app.MapHub<TelemetryHub>("/hubs/telemetry");

app.MapControllers();

app.Run();
