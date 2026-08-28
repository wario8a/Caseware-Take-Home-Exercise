using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Collaborate.Api.Observability;

public static class CollaborateObservability
{
    public const string MeterName = "Collaborate.Api";
    public const string ActivitySourceName = "Collaborate.Api";

    public static readonly Meter Meter = new(MeterName);
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
