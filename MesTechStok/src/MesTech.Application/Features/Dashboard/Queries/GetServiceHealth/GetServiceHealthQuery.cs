using MesTech.Application.Behaviors;
using MesTech.Application.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;

/// <summary>
/// Altyapi servisleri saglik durumu sorgusu — PostgreSQL, Redis, RabbitMQ.
/// Cache: 30 saniye (servis durumu sik kontrol edilir).
/// G308: AppHub mock→real data.
/// </summary>
public record GetServiceHealthQuery
    : IRequest<IReadOnlyList<ServiceHealthDto>>, ICacheableQuery
{
    public string CacheKey => "ServiceHealth_Infra";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
}

public record ServiceHealthDto
{
    public string ServiceName { get; init; } = string.Empty;
    public bool IsHealthy { get; init; }
    public string ResponseTime { get; init; } = "—";
}
