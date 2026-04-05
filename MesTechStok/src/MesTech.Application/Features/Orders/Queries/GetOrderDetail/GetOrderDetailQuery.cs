using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Orders.Queries.GetOrderDetail;

public record GetOrderDetailQuery(Guid TenantId, Guid OrderId) : IRequest<OrderDetailDto?>;

public sealed class OrderDetailDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerEmail { get; init; }
    public string? ShippingAddress { get; init; }
    public DateTime OrderDate { get; init; }
    public decimal SubTotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string? TrackingNumber { get; init; }
    public string? CargoProvider { get; init; }
    public string? Notes { get; init; }
    public PlatformType? SourcePlatform { get; init; }
    public string? PaymentStatus { get; init; }

    // Timeline — OrderTimelineControl için gerekli
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }

    public IReadOnlyList<OrderLineItemDto> LineItems { get; init; } = [];
}

public sealed class OrderLineItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string SKU { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public decimal TaxAmount { get; init; }
}
