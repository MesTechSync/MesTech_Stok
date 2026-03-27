using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetPendingInvoices;

public record GetPendingInvoicesQuery(Guid TenantId, int Count = 10)
    : IRequest<IReadOnlyList<PendingInvoiceDto>>, ICacheableQuery
{
    public string CacheKey => $"PendingInvoices_{TenantId}_{Count}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public sealed class PendingInvoiceDto
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public decimal GrandTotal { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int DaysPending { get; init; }
}
