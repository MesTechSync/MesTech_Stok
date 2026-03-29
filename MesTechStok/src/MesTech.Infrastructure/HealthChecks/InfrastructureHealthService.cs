using System.Diagnostics;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.HealthChecks;

/// <summary>
/// PostgreSQL, Redis, RabbitMQ saglik kontrollerini toplu calistirir.
/// G308: AppHub mock→real data icin IInfrastructureHealthService implementasyonu.
/// </summary>
public sealed class InfrastructureHealthService : IInfrastructureHealthService
{
    private readonly PostgresHealthCheck _postgres;
    private readonly RedisHealthCheck _redis;
    private readonly RabbitMqHealthCheck _rabbit;
    private readonly ILogger<InfrastructureHealthService> _logger;

    public InfrastructureHealthService(
        PostgresHealthCheck postgres,
        RedisHealthCheck redis,
        RabbitMqHealthCheck rabbit,
        ILogger<InfrastructureHealthService> logger)
    {
        _postgres = postgres;
        _redis = redis;
        _rabbit = rabbit;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ServiceHealthResult>> CheckAllAsync(CancellationToken ct = default)
    {
        var checks = new (string Name, IHealthCheck Check)[]
        {
            ("PostgreSQL", _postgres),
            ("Redis", _redis),
            ("RabbitMQ", _rabbit)
        };

        var results = new List<ServiceHealthResult>(checks.Length);

        foreach (var (name, check) in checks)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var result = await check.CheckHealthAsync(
                    new HealthCheckContext(), ct);
                sw.Stop();

                results.Add(new ServiceHealthResult(
                    name,
                    result.Status == HealthStatus.Healthy,
                    $"{sw.ElapsedMilliseconds}ms",
                    result.Status != HealthStatus.Healthy ? result.Description : null));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check failed for {Service}", name);
                results.Add(new ServiceHealthResult(
                    name,
                    IsHealthy: false,
                    ErrorMessage: ex.Message));
            }
        }

        return results.AsReadOnly();
    }
}
