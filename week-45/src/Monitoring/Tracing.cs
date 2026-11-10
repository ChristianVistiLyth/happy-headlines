using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Monitoring;

/// <summary>
/// Our own spans, and carrying a trace across a queue.
/// For HTTP calls HttpClient adds the traceparent header by itself. A queue message has no such header,
/// so the sender Injects the trace context into the message, and the receiver Extracts it and continues
/// the same trace instead of starting a new one.
/// </summary>
public static class Tracing
{
    public const string SourceName = "HappyHeadlines";

    /// <summary>Starts our own spans, e.g. "publish article" or "send newsletter".</summary>
    public static readonly ActivitySource Source = new(SourceName);

    // Writes and reads the W3C "traceparent" value; OpenTelemetry sets it up when the service starts
    private static TextMapPropagator Propagator => Propagators.DefaultTextMapPropagator;

    /// <summary>Sender side: writes the trace context of <paramref name="activity"/> into the message headers.</summary>
    public static void Inject(Activity? activity, IDictionary<string, string> headers) =>
        Propagator.Inject(new PropagationContext(activity?.Context ?? default, Baggage.Current), headers,
            (carrier, key, value) => carrier[key] = value);

    /// <summary>
    /// Receiver side: reads the trace context from the message headers and starts a span
    /// that continues that trace, so the publisher and the receiver end up in ONE trace.
    /// </summary>
    public static Activity? StartReceive(string name, IDictionary<string, string> headers)
    {
        var parent = Propagator.Extract(default, headers,
            (carrier, key) => carrier.TryGetValue(key, out var value) ? new[] { value } : null);
        return Source.StartActivity(name, ActivityKind.Consumer, parent.ActivityContext);
    }
}
