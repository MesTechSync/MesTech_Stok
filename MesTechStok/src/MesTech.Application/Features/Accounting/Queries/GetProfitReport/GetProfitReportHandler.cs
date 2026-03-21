using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Application.Features.Accounting.Queries.GetProfitReport;

public class GetProfitReportHandler : IRequestHandler<GetProfitReportQuery, ProfitReportDto?>
{
    private readonly IProfitReportRepository _repository;
    private readonly IProfitCalculationService _profitService;

    public GetProfitReportHandler(IProfitReportRepository repository, IProfitCalculationService profitService)
    {
        _repository = repository;
        _profitService = profitService;
    }

    public async Task<ProfitReportDto?> Handle(GetProfitReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reports = await _repository.GetByPeriodAsync(request.TenantId, request.Period, request.Platform, cancellationToken);
        var report = reports.Count > 0 ? reports[0] : null;

        if (report == null) return null;

        return new ProfitReportDto
        {
            Id = report.Id,
            ReportDate = report.ReportDate,
            Platform = report.Platform,
            TotalRevenue = report.TotalRevenue,
            TotalCost = report.TotalCost,
            TotalCommission = report.TotalCommission,
            TotalCargo = report.TotalCargo,
            TotalTax = report.TotalTax,
            NetProfit = report.NetProfit,
            ProfitMargin = _profitService.CalculateProfitMargin(report.TotalRevenue, report.NetProfit),
            Period = report.Period
        };
    }
}
