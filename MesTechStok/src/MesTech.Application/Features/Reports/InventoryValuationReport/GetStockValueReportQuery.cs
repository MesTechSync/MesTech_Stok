using MediatR;
using MesTech.Application.DTOs.Reports;

namespace MesTech.Application.Features.Reports.InventoryValuationReport;

/// <summary>
/// Alias for InventoryValuationReportQuery — used by StockValueReportViewModel.
/// Returns per-product stock valuation with cost, sale price and potential profit.
/// </summary>
public record GetStockValueReportQuery(
    Guid TenantId,
    Guid? CategoryFilter = null
) : IRequest<StockValueReportResult>;

/// <summary>
/// Summary result wrapping individual product valuations + totals.
/// </summary>
public sealed class StockValueReportResult
{
    public IReadOnlyList<InventoryValuationReportDto> Items { get; init; } = [];
    public decimal TotalCostValue { get; init; }
    public decimal TotalSaleValue { get; init; }
    public decimal TotalPotentialProfit { get; init; }
    public int TotalProducts { get; init; }
    public int TotalStockUnits { get; init; }
}
