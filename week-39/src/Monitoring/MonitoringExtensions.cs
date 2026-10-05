using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Monitoring;

/// <summary>
/// Central logging and tracing, reused by every service with one line: builder.AddMonitoring().
/// Logs and traces are sent with OpenTelemetry (OTLP) to the address in OTEL_EXPORTER_OTLP_ENDPOINT,
/// where the monitoring stack stores them (logs in Loki, traces in Tempo) and Grafana shows them.
/// </summary>
public static class MonitoringExtensions
{
    public static WebApplicationBuilder AddMonitoring(this WebApplicationBuilder builder)
    {
        // Every log line and trace says which service, and which instance of it, it came from
        var serviceName = builder.Environment.ApplicationName;
        var instanceName = builder.Configuration["InstanceName"] ?? Environment.MachineName;

        // Logging policy: our own code logs from Information, the framework only from Warning,
        // so important lines are not drowned in framework details
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("System", LogLevel.Warning);
        builder.Logging.AddFilter("EasyNetQ", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);

        // Send the finished sentence ("Draft 42 deleted"), not only the template ("Draft {DraftId} deleted")
        builder.Services.Configure<OpenTelemetryLoggerOptions>(options => options.IncludeFormattedMessage = true);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName, serviceInstanceId: instanceName))
            .WithLogging()
            .WithTracing(tracing => tracing
                // Incoming HTTP requests - except the API docs pages, which are not system traffic
                .AddAspNetCoreInstrumentation(options => options.Filter = context =>
                    !context.Request.Path.StartsWithSegments("/swagger") &&
                    !context.Request.Path.StartsWithSegments("/openapi"))
                .AddHttpClientInstrumentation()   // outgoing HTTP calls to other services
                .AddSource("Npgsql")              // database queries
                .AddSource(Tracing.SourceName))   // our own spans, e.g. publishing to and receiving from the ArticleQueue
            .UseOtlpExporter();

        return builder;
    }
}
