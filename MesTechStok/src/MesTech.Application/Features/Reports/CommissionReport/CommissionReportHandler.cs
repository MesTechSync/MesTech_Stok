using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Reports.CommissionReport;

public class CommissionReportHandler : IRequestHandler<CommissionReportQuery, CommissionReportDto>
{
    private readonly ICommissionRecordRepository _commissionRepo;

    public CommissionReportHandler(ICommissionRecordRepository commissionRepo)
        => _commissionRepo = commissionRepo;

    public async Task<CommissionReportDto> Handle(CommissionReportQuery request, CancellationToken cancellationToken)
    {
        var platforms = request.PlatformFilter.HasValue
            ? new[] { request.PlatformFilter.Value.ToString() }
            : new[] { "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama", "eBay", "Shopify", "WooCommerce" };

        var breakdown = new List<PlatformCommissionBreakdownDto>();
        decimal totalCommission = 0;
        decimal totalOrderAmount = 0;
        int totalOrderCount = 0;

        foreach (var platform in platforms)
        {
            var records = await _commissionRepo.GetByPlatformAsync(
                request.TenantId, platform, request.StartDate, request.EndDate, cancellationToken);
            if (records.Count == 0) continue;

            var commissionAmount = records.Sum(r => r.CommissionAmount);
            var grossAmount = records.Sum(r => r.GrossAmount);
            var rate = grossAmount > 0 ? Math.Round(commissionAmount / grossAmount * 100, 2) : 0;

            if (Enum.TryParse<PlatformType>(platform, true, out var platformType))
            {
                breakdown.Add(new PlatformCommissionBreakdownDto(
                    platformType, platform, records.Count, grossAmount,
                    commissionAmount, rate, grossAmount - commissionAmount));
            }

            totalCommission += commissionAmount;
            totalOrderAmount += grossAmount;
            totalOrderCount += records.Count;
        }

        // Period comparison — same duration in previous period
        var duration = request.EndDate - request.StartDate;
        var prevStart = request.StartDate - duration;
        var prevEnd = request.StartDate;
        decimal prevCommission = 0;

        foreach (var platform in platforms)
        {
            var prevRecords = await _commissionRepo.GetByPlatformAsync(
                request.TenantId, platform, prevStart, prevEnd, cancellationToken);
            prevCommission += prevRecords.Sum(r => r.CommissionAmount);
        }

        var comparison = prevCommission > 0
            ? new PeriodComparisonDto(prevCommission,
                Math.Round((totalCommission - prevCommission) / prevCommission * 100, 2))
            : null;

        return new CommissionReportDto
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalCommission = totalCommission,
            TotalOrderAmount = totalOrderAmount,
            TotalOrderCount = totalOrderCount,
            AverageCommissionRate = totalOrderAmount > 0
                ? Math.Round(totalCommission / totalOrderAmount * 100, 2) : 0,
            PlatformBreakdown = breakdown,
            PeriodComparison = comparison
        };
    }
}
