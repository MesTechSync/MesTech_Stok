using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Reports.PlatformPerformanceReport;

public record PlatformPerformanceReportQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<PlatformPerformanceReportDto>;

public record PlatformPerformanceReportDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public IReadOnlyList<PlatformMetricsDto> Platforms { get; init; } = [];
    public PlatformType? BestPlatform { get; init; }
    public PlatformType? WorstPlatform { get; init; }
}

public record PlatformMetricsDto(
    PlatformType PlatformType,
    string PlatformName,
    int OrderCount,
    decimal Revenue,
    int ReturnCount,
    decimal ReturnRate,
    decimal AverageOrderValue,
    decimal CommissionPaid,
    decimal NetRevenue,
    decimal Score);
