using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;

namespace MesTech.Application.Commands.PlaceOrder;

public sealed class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StockCalculationService _stockCalculation;

    public PlaceOrderHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        StockCalculationService stockCalculation)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _stockCalculation = stockCalculation ?? throw new ArgumentNullException(nameof(stockCalculation));
    }

    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Notes = request.Notes,
            OrderDate = DateTime.UtcNow,
        };

        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId).ConfigureAwait(false);
            if (product == null)
                return new PlaceOrderResult { IsSuccess = false, ErrorMessage = $"Product {item.ProductId} not found." };

            _stockCalculation.ValidateStockSufficiency(product, item.Quantity);

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSKU = product.SKU,
                TaxRate = item.TaxRate
            };
            orderItem.SetQuantityAndPrice(item.Quantity, item.UnitPrice);
            order.AddItem(orderItem);
            product.AdjustStock(-item.Quantity, StockMovementType.Sale);
        }

        order.CalculateTotals();
        order.Place();

        await _orderRepository.AddAsync(order).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PlaceOrderResult
        {
            IsSuccess = true,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber
        };
    }
}
