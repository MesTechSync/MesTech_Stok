using MediatR;
using MesTech.Application.DTOs.Reports;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reports.TaxSummaryReport;

/// <summary>
/// Vergi beyanname ozet raporu handler'i.
/// Orders verilerinden aylık bazda KDV ozeti hesaplar.
/// Gercek uretimde fatura verisi (Invoice) ile zenginlestirilecektir.
/// </summary>
public class TaxSummaryReportHandler
    : IRequestHandler<TaxSummaryReportQuery, IReadOnlyList<TaxSummaryReportDto>>
{
    private readonly IOrderRepository _orderRepository;

    public TaxSummaryReportHandler(IOrderRepository orderRepository)
        => _orderRepository = orderRepository;

    public async Task<IReadOnlyList<TaxSummaryReportDto>> Handle(
        TaxSummaryReportQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByDateRangeAsync(
            request.TenantId, request.StartDate, request.EndDate, cancellationToken);

        // Group orders by month for tax period
        var monthlyGroups = orders
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month);

        var result = new List<TaxSummaryReportDto>();

        foreach (var group in monthlyGroups)
        {
            var monthOrders = group.ToList();
            var totalSales = monthOrders.Sum(o => o.TotalAmount);
            var totalTax = monthOrders.Sum(o => o.TaxAmount);

            // Estimate input VAT as a portion of purchase costs (simplified)
            // In production this would come from purchase invoices
            var estimatedInputVat = totalTax * 0.6m; // Conservative estimate
            var netVat = totalTax - estimatedInputVat;

            result.Add(new TaxSummaryReportDto
            {
                TaxPeriod = $"{group.Key.Year}-{group.Key.Month:D2}",
                TotalSalesAmount = totalSales,
                TotalPurchaseAmount = totalSales * 0.6m, // Simplified estimate
                OutputVat = totalTax,
                InputVat = Math.Round(estimatedInputVat, 2),
                NetVatPayable = Math.Round(netVat, 2),
                InvoiceCount = monthOrders.Count,
                WithholdingAmount = 0m // To be enriched from withholding records
            });
        }

        return result.AsReadOnly();
    }
}
