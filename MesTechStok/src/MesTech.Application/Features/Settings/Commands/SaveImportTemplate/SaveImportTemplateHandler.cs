using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Settings.Commands.SaveImportTemplate;

/// <summary>
/// Import sablonu kaydetme handler'i.
/// Yeni sablon olusturur ve kolon eslestirmelerini ekler.
/// </summary>
public sealed class SaveImportTemplateHandler : IRequestHandler<SaveImportTemplateCommand, SaveImportTemplateResult>
{
    private readonly IImportTemplateRepository _templateRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaveImportTemplateHandler> _logger;

    public SaveImportTemplateHandler(
        IImportTemplateRepository templateRepo,
        IUnitOfWork unitOfWork,
        ILogger<SaveImportTemplateHandler> logger)
    {
        _templateRepo = templateRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SaveImportTemplateResult> Handle(
        SaveImportTemplateCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var template = ImportTemplate.Create(
                request.TenantId,
                request.TemplateName,
                request.FileFormat);

            foreach (var mapping in request.ColumnMappings)
            {
                template.AddMapping(mapping.Key, mapping.Value);
            }

            await _templateRepo.AddAsync(template, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Import sablonu kaydedildi: {TemplateId}, Ad: {Name}, Format: {Format}, Kolon: {Count}",
                template.Id, request.TemplateName, request.FileFormat, request.ColumnMappings.Count);

            return SaveImportTemplateResult.Success(template.Id);
        }
#pragma warning disable CA1031 // Catch general exception — return structured error instead of throwing
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Import sablonu kaydetme hatasi: {Name}", request.TemplateName);
            return SaveImportTemplateResult.Failure(ex.Message);
        }
    }
}
