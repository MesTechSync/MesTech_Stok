using MediatR;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands.CreateAutoOrder;

/// <summary>
/// Minimum stok altındaki ürünler için DropshipOrder oluşturur.
/// Product.Stock &lt; Product.MinimumStock kontrolü yapılır.
/// </summary>
public sealed class CreateAutoOrderHandler : IRequestHandler<CreateAutoOrderCommand, AutoOrderResultDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IDropshipSupplierRepository _supplierRepository;
    private readonly IDropshipOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAutoOrderHandler(
        IProductRepository productRepository,
        IDropshipSupplierRepository supplierRepository,
        IDropshipOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AutoOrderResultDto> Handle(
        CreateAutoOrderCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ProductIds is null || request.ProductIds.Count == 0)
            throw new ArgumentException("At least one product ID must be provided.", nameof(request));

        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
            ?? throw new InvalidOperationException($"Supplier '{request.SupplierId}' not found.");

        // Batch query — N+1 yerine tek SQL
        var allProducts = await _productRepository.GetByIdsAsync(request.ProductIds, cancellationToken).ConfigureAwait(false);
        var productMap = allProducts.ToDictionary(p => p.Id);

        var orders = new List<AutoOrderItemDto>();
        decimal totalAmount = 0;

        foreach (var productId in request.ProductIds)
        {
            if (!productMap.TryGetValue(productId, out var product))
                continue;

            // Sadece minimum stok altındaki ürünler için sipariş oluştur
            if (product.Stock >= product.MinimumStock)
                continue;

            var dropshipOrder = DropshipOrder.Create(
                tenantId: product.TenantId,
                orderId: Guid.NewGuid(), // Yeni iç sipariş referansı
                supplierId: supplier.Id,
                productId: productId);

            // AutoApprove aktifse hemen "ordered" durumuna geçir
            if (request.AutoApprove)
            {
                dropshipOrder.PlaceWithSupplier($"AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}-{productId.ToString()[..8]}");
            }

            await _orderRepository.AddAsync(dropshipOrder, cancellationToken).ConfigureAwait(false);

            totalAmount += product.PurchasePrice * product.ReorderQuantity;

            orders.Add(new AutoOrderItemDto
            {
                OrderId = dropshipOrder.Id,
                ProductId = productId,
                ProductName = product.Name,
                CurrentStock = product.Stock,
                MinimumStock = product.MinimumStock
            });
        }

        if (orders.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AutoOrderResultDto
        {
            OrdersCreated = orders.Count,
            TotalAmount = totalAmount,
            Orders = orders
        };
    }
}
