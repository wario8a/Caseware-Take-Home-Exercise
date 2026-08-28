using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Collaborate.Api.Observability;

public static class CollaborateObservability
{
    public const string MeterName = "Collaborate.Api";
    public const string ActivitySourceName = "Collaborate.Api";

    public static readonly Meter Meter = new(MeterName);
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Counter<long> DocumentReadRequests =
        Meter.CreateCounter<long>("collaborate.documents.read.requests");
    public static readonly Counter<long> DocumentReadFailures =
        Meter.CreateCounter<long>("collaborate.documents.read.failures");
    public static readonly Histogram<double> DocumentReadDuration =
        Meter.CreateHistogram<double>("collaborate.documents.read.duration", unit: "ms");
}
