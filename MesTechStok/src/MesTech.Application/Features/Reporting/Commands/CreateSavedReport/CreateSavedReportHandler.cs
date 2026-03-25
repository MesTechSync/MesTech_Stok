using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Reporting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Reporting.Commands.CreateSavedReport;

/// <summary>
/// Yeni kaydedilmis rapor olusturma handler'i.
/// SavedReport entity olusturur ve veritabanina kaydeder.
/// </summary>
public sealed class CreateSavedReportHandler : IRequestHandler<CreateSavedReportCommand, Guid>
{
    private readonly ISavedReportRepository _savedReportRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateSavedReportHandler> _logger;

    public CreateSavedReportHandler(
        ISavedReportRepository savedReportRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateSavedReportHandler> logger)
    {
        _savedReportRepository = savedReportRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateSavedReportCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var savedReport = SavedReport.Create(
            request.TenantId,
            request.Name,
            request.ReportType,
            request.FilterJson,
            request.CreatedByUserId);

        await _savedReportRepository.AddAsync(savedReport, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Kaydedilmis rapor olusturuldu: Id={ReportId}, Name={Name}, Type={ReportType}",
            savedReport.Id, request.Name, request.ReportType);

        return savedReport.Id;
    }
}
