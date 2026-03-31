using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;

/// <summary>
/// Kaydedilmis rapor silme handler'i.
/// Tenant dogrulamasi yaparak raporu siler.
/// </summary>
public sealed class DeleteSavedReportHandler : IRequestHandler<DeleteSavedReportCommand, bool>
{
    private readonly ISavedReportRepository _savedReportRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteSavedReportHandler> _logger;

    public DeleteSavedReportHandler(
        ISavedReportRepository savedReportRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteSavedReportHandler> logger)
    {
        _savedReportRepository = savedReportRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteSavedReportCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var report = await _savedReportRepository.GetByIdAsync(request.ReportId, cancellationToken).ConfigureAwait(false);

        if (report is null || report.TenantId != request.TenantId)
        {
            _logger.LogWarning(
                "Kaydedilmis rapor bulunamadi veya tenant uyumsuz: ReportId={ReportId}, TenantId={TenantId}",
                request.ReportId, request.TenantId);
            return false;
        }

        await _savedReportRepository.DeleteAsync(report, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Kaydedilmis rapor silindi: Id={ReportId}, Name={Name}",
            report.Id, report.Name);

        return true;
    }
}
