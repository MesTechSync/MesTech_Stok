using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetLowStockAlerts;

public sealed class GetLowStockAlertsHandler
    : IRequestHandler<GetLowStockAlertsQuery, IReadOnlyList<LowStockAlertDto>>
{
    private readonly IProductRepository _productRepo;

    public GetLowStockAlertsHandler(IProductRepository productRepo)
        => _productRepo = productRepo ?? throw new ArgumentNullException(nameof(productRepo));

    public async Task<IReadOnlyList<LowStockAlertDto>> Handle(
        GetLowStockAlertsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepo.GetLowStockAsync(cancellationToken)
            .ConfigureAwait(false);

        return products
            .Take(request.Count)
            .Select(p => new LowStockAlertDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                SKU = p.SKU,
                CurrentStock = p.Stock,
                MinimumStock = p.MinimumStock,
                Deficit = p.MinimumStock - p.Stock,
                Severity = p.Stock == 0 ? "Critical" : "Warning"
            })
            .ToList();
    }
}
