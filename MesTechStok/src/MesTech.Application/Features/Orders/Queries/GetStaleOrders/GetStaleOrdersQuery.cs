using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Orders.Queries.GetStaleOrders;

/// <summary>
/// 48 saat+ onaylı ama gönderilmemiş siparişleri sorgular.
/// Hangfire recurring job tarafından saatlik çağrılır.
/// </summary>
public record GetStaleOrdersQuery(
    Guid TenantId,
    TimeSpan Threshold = default) : IRequest<IReadOnlyList<StaleOrderDto>>
{
    public TimeSpan EffectiveThreshold => Threshold == default ? TimeSpan.FromHours(48) : Threshold;
}

public record StaleOrderDto(
    Guid OrderId,
    string OrderNumber,
    PlatformType? Platform,
    DateTime CreatedAt,
    TimeSpan Elapsed,
    string? CustomerName);
