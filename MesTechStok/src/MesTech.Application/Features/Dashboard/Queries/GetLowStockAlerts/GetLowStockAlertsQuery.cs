using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetLowStockAlerts;

public record GetLowStockAlertsQuery(Guid TenantId, int Count = 20)
    : IRequest<IReadOnlyList<LowStockAlertDto>>, ICacheableQuery
{
    public string CacheKey => $"LowStockAlerts_{TenantId}_{Count}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed class LowStockAlertDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string SKU { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int MinimumStock { get; init; }
    public int Deficit { get; init; }
    public string Severity { get; init; } = "Warning";
}
