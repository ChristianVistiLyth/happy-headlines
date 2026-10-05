using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Monitoring;

/// <summary>
/// Counts cache hits and misses, and how long each read took, for the cache hit-ratio dashboard.
/// The numbers are sent with OpenTelemetry to Prometheus, and Grafana shows them.
/// </summary>
public static class CacheMetrics
{
    public const string MeterName = "HappyHeadlines.Cache";

    private static readonly Meter Meter = new(MeterName);

    // One count per read, labelled with the cache ("article" or "comment") and the result ("hit" or "miss")
    private static readonly Counter<long> Lookups = Meter.CreateCounter<long>("cache.lookups",
        description: "Reads that looked in the cache first, labelled hit or miss");

    // How long the whole read took - a miss includes the trip to the database
    private static readonly Histogram<double> ReadDuration = Meter.CreateHistogram<double>("cache.read.duration",
        unit: "s", description: "How long a read took, labelled hit or miss");

    /// <summary>Records one read: which cache, whether it was a hit, and how long it took.</summary>
    public static void Record(string cache, bool hit, TimeSpan elapsed)
    {
        var tags = new TagList { { "cache", cache }, { "result", hit ? "hit" : "miss" } };
        Lookups.Add(1, tags);
        ReadDuration.Record(elapsed.TotalSeconds, tags);
    }
}
