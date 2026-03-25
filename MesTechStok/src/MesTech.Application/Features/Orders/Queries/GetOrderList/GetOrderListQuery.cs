using MediatR;

namespace MesTech.Application.Features.Orders.Queries.GetOrderList;

/// <summary>
/// Recent orders list query — used by OrderListView (Avalonia).
/// </summary>
public record GetOrderListQuery(
    Guid TenantId,
    int Count = 100
) : IRequest<IReadOnlyList<OrderListItemDto>>;

public sealed class OrderListItemDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentStatus { get; set; }
    public string? SourcePlatform { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
    public DateTime OrderDate { get; set; }
    public string? TrackingNumber { get; set; }
}
