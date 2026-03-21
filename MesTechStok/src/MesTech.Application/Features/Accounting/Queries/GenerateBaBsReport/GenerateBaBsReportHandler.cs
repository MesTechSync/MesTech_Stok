using MediatR;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GenerateBaBsReport;

/// <summary>
/// Ba/Bs beyanname raporu handler.
/// IBaBsReportService uzerinden donem bazinda Ba/Bs formlarini olusturur.
/// Ba: 5.000 TL ustu alislar — tedarikci bazli.
/// Bs: 5.000 TL ustu satislar — musteri bazli.
/// </summary>
public class GenerateBaBsReportHandler
    : IRequestHandler<GenerateBaBsReportQuery, BaBsReportDto>
{
    private readonly IBaBsReportService _babsService;

    public GenerateBaBsReportHandler(IBaBsReportService babsService)
    {
        _babsService = babsService;
    }

    public async Task<BaBsReportDto> Handle(
        GenerateBaBsReportQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _babsService.GenerateBaBsReportAsync(
            request.TenantId,
            request.Year,
            request.Month,
            cancellationToken);
    }
}
