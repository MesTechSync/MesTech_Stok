namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Rate limit telemetry for adapter connection test panels.
/// </summary>
public sealed record RateLimitInfo(
    int ConcurrentSlots,
    int MaxConcurrentSlots,
    int TotalRequests,
    int ThrottledRequests,
    DateTime? LastThrottleAt)
{
    public double ThrottlePercentage =>
        TotalRequests > 0 ? (double)ThrottledRequests / TotalRequests * 100 : 0;

    public double UsagePercentage =>
        MaxConcurrentSlots > 0 ? (double)(MaxConcurrentSlots - ConcurrentSlots) / MaxConcurrentSlots * 100 : 0;
}
