using MediatR;
using MesTech.Application.DTOs.Reports;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reports.InventoryValuationReport;

/// <summary>
/// Envanter degerleme raporu handler'i.
/// Product verilerini ceker, stok x maliyet hesabi yapar ve kategori filtrelemesi uygular.
/// </summary>
public class InventoryValuationReportHandler
    : IRequestHandler<InventoryValuationReportQuery, IReadOnlyList<InventoryValuationReportDto>>
{
    private readonly IProductRepository _productRepository;

    public InventoryValuationReportHandler(IProductRepository productRepository)
        => _productRepository = productRepository;

    public async Task<IReadOnlyList<InventoryValuationReportDto>> Handle(
        InventoryValuationReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var products = request.CategoryFilter.HasValue
            ? await _productRepository.GetByCategoryAsync(request.CategoryFilter.Value)
            : await _productRepository.GetAllAsync();

        var result = products
            .Where(p => p.Stock > 0)
            .Select(p =>
            {
                var totalCost = p.Stock * p.PurchasePrice;
                var totalSale = p.Stock * p.SalePrice;

                return new InventoryValuationReportDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    SKU = p.SKU,
                    CurrentStock = p.Stock,
                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice,
                    TotalCostValue = totalCost,
                    TotalSaleValue = totalSale,
                    PotentialProfit = totalSale - totalCost
                };
            })
            .OrderByDescending(r => r.TotalCostValue)
            .ToList()
            .AsReadOnly();

        return result;
    }
}
