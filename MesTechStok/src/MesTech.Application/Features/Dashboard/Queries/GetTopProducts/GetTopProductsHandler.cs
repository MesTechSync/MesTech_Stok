using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetTopProducts;

/// <summary>
/// En cok satan urunler isleyicisi.
/// Son 30 gundeki siparis kalemlerini urun bazinda gruplar,
/// gelire gore azalan sirada sıralar, limit kadar dondurur.
/// </summary>
public class GetTopProductsHandler : IRequestHandler<GetTopProductsQuery, IReadOnlyList<TopProductDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetTopProductsHandler(IOrderRepository orderRepository)
        => _orderRepository = orderRepository;

    public async Task<IReadOnlyList<TopProductDto>> Handle(
        GetTopProductsQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        var orders = await _orderRepository.GetByDateRangeWithItemsAsync(
            request.TenantId, from, to, cancellationToken);

        var topProducts = orders
            .SelectMany(o => o.OrderItems)
            .GroupBy(item => item.ProductId)
            .Select(g => new TopProductDto
            {
                ProductId = g.Key,
                SKU = g.First().ProductSKU,
                Name = g.First().ProductName,
                SoldQuantity = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(limit)
            .ToList();

        return topProducts.AsReadOnly();
    }
}
