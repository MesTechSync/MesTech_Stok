using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces.Dropshipping;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;

public sealed class GetDropshipProfitabilityHandler
    : IRequestHandler<GetDropshipProfitabilityQuery, List<DropshipProfitDto>>
{
    private readonly IDropshipOrderRepository _orderRepository;
    private readonly IDropshipProductRepository _productRepository;

    public GetDropshipProfitabilityHandler(
        IDropshipOrderRepository orderRepository,
        IDropshipProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<List<DropshipProfitDto>> Handle(
        GetDropshipProfitabilityQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var orders = await _orderRepository
            .GetByTenantAsync(request.TenantId, cancellationToken);

        var products = await _productRepository
            .GetByTenantAsync(request.TenantId, ct: cancellationToken);

        var productLookup = products.ToDictionary(p => p.Id);

        // Group orders by product
        var grouped = orders
            .Where(o => productLookup.ContainsKey(o.DropshipProductId))
            .GroupBy(o => o.DropshipProductId)
            .Select(g =>
            {
                var product = productLookup[g.Key];
                var quantitySold = g.Count();
                var customerPrice = product.SellingPrice;
                var supplierPrice = product.OriginalPrice;
                // Commission is estimated as 0 here — requires platform commission rate lookup
                var commission = 0m;
                var netProfit = (customerPrice - supplierPrice - commission) * quantitySold;
                var margin = customerPrice > 0
                    ? (customerPrice - supplierPrice - commission) / customerPrice * 100m
                    : 0m;

                return new DropshipProfitDto
                {
                    ProductId = g.Key,
                    ProductName = product.Title,
                    QuantitySold = quantitySold,
                    CustomerPrice = customerPrice,
                    SupplierPrice = supplierPrice,
                    CommissionAmount = commission,
                    NetProfit = netProfit,
                    ProfitMargin = Math.Round(margin, 2)
                };
            })
            .OrderByDescending(d => d.NetProfit)
            .ToList();

        return grouped;
    }
}
