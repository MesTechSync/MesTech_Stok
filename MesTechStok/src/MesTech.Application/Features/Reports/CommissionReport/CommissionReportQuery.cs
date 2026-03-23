using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Reports.CommissionReport;

public record CommissionReportQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate,
    PlatformType? PlatformFilter = null
) : IRequest<CommissionReportDto>;

public record CommissionReportDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal TotalOrderAmount { get; init; }
    public int TotalOrderCount { get; init; }
    public decimal AverageCommissionRate { get; init; }
    public IReadOnlyList<PlatformCommissionBreakdownDto> PlatformBreakdown { get; init; } = [];
    public PeriodComparisonDto? PeriodComparison { get; init; }
}

public record PlatformCommissionBreakdownDto(
    PlatformType PlatformType,
    string PlatformName,
    int OrderCount,
    decimal OrderAmount,
    decimal CommissionAmount,
    decimal CommissionRate,
    decimal NetRevenue);

public record PeriodComparisonDto(
    decimal PreviousPeriodCommission,
    decimal ChangePercent);
