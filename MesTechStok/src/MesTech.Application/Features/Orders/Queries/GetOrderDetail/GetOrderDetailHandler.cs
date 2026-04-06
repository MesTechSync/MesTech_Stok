using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Orders.Queries.GetOrderDetail;

public sealed class GetOrderDetailHandler : IRequestHandler<GetOrderDetailQuery, OrderDetailDto?>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<GetOrderDetailHandler> _logger;

    public GetOrderDetailHandler(IOrderRepository orderRepository, ILogger<GetOrderDetailHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<OrderDetailDto?> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null) return null;

        if (order.TenantId != request.TenantId)
        {
            _logger.LogWarning("Order {OrderId} does not belong to tenant {TenantId}", request.OrderId, request.TenantId);
            return null;
        }

        return new OrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            ShippingAddress = order.ShippingAddress,
            OrderDate = order.OrderDate,
            SubTotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            TrackingNumber = order.TrackingNumber,
            CargoProvider = order.CargoProvider?.ToString(),
            Notes = order.Notes,
            SourcePlatform = order.SourcePlatform,
            PaymentStatus = order.PaymentStatus,
            ConfirmedAt = order.ConfirmedAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            LineItems = order.OrderItems.Select(i => new OrderLineItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                SKU = i.ProductSKU,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                TaxAmount = i.TaxAmount
            }).ToList()
        };
    }
}
