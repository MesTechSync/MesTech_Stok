namespace MesTech.Application.Interfaces;

/// <summary>
/// Altyapi servislerinin saglik durumunu kontrol eder (PostgreSQL, Redis, RabbitMQ).
/// Infrastructure katmaninda implemente edilir.
/// </summary>
public interface IInfrastructureHealthService
{
    Task<IReadOnlyList<ServiceHealthResult>> CheckAllAsync(CancellationToken ct = default);
}

public sealed record ServiceHealthResult(
    string ServiceName,
    bool IsHealthy,
    string? ResponseTimeMs = null,
    string? ErrorMessage = null);
