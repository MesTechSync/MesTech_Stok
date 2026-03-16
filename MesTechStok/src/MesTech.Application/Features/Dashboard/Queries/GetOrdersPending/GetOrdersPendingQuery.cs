using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetOrdersPending;

/// <summary>
/// Bekleyen siparis sayisi sorgusu — Pending/Confirmed durumdaki siparisler.
/// </summary>
public record GetOrdersPendingQuery(Guid TenantId)
    : IRequest<OrdersPendingDto>;

/// <summary>
/// Bekleyen siparis ozet DTO.
/// </summary>
public record OrdersPendingDto
{
    public int Count { get; init; }
    public int Urgent { get; init; }
    public int OldestMinutes { get; init; }
}
