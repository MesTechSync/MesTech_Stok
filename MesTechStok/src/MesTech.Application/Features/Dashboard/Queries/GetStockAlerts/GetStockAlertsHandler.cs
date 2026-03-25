using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;

/// <summary>
/// Dusuk stok uyari isleyicisi.
/// Stock <= MinimumStock olan aktif urunleri listeler.
/// </summary>
public sealed class GetStockAlertsHandler : IRequestHandler<GetStockAlertsQuery, IReadOnlyList<StockAlertDto>>
{
    private readonly IProductRepository _productRepository;

    public GetStockAlertsHandler(IProductRepository productRepository)
        => _productRepository = productRepository;

    public async Task<IReadOnlyList<StockAlertDto>> Handle(GetStockAlertsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lowStockProducts = await _productRepository.GetLowStockAsync();

        return lowStockProducts
            .Select(p => new StockAlertDto
            {
                ProductId = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                CurrentStock = p.Stock,
                MinThreshold = p.MinimumStock,
                Platform = p.PlatformMappings.FirstOrDefault()?.PlatformType.ToString()
            })
            .ToList()
            .AsReadOnly();
    }
}
