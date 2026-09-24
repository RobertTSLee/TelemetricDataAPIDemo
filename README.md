Case Study

# Case Study: High-Throughput Ingestion and Concurrency APIs for Telemetry Data Streams
Create high-thoughout Ingestion and Concurrecny APIs that receive telemetry data steams every 200ms,
process this asynchronously without exhausting thread pool threads or blocking the database.

Problem Statement
“Design an Alerting & Telemetry Notification Service for high-frequency industrial sensors. When a pressure gauge exceeds a set threshold, thousands of operators and automation hooks need to be notified in under 2 seconds.”

Key Points:
1. Utilize Aysnc/Await and non-blocking I/O operations to handle incoming telemetry data streams efficiently.
2. Use Channel to decouple HTTP ingestion 
3. Batching: grouping imcoming payloads in memory and processing them in batches to reduce database load and improve throughput.

Expectation Breakdown
1. API Ingestion: Fast, stateless REST or gRPC endpoint in .NET Core that validates and pushes data to an event bus (e.g., Azure Event Hubs or Kafka).
2. Event Processing: A background worker service (IHostedService or Azure Function) consuming batches from the queue.
3. Real-Time Client Pushes: Utilizing ASP.NET Core SignalR backplane (via Azure SignalR Service) to fan out alerts to connected Angular client dashboards.
4. Angular Dashboard Handling: Angular client consuming SignalR events, maintaining state with Signals, and triggering UI toasts/alerts without re-rendering the entire dashboard layout.



Local Postgres Database Container
docker run -d --name postgres16_container -e POSTGRES_DB=postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -v pgdata:/var/lib/postgresql/data postgres:16
