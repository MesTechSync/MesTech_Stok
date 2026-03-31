using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Crm.Commands.SaveCrmSettings;

/// <summary>
/// CRM ayarlarini CompanySettings uzerinde kaydeder.
/// Tenant yoksa yeni kayit olusturur, varsa gunceller.
/// </summary>
public sealed class SaveCrmSettingsHandler : IRequestHandler<SaveCrmSettingsCommand, SaveCrmSettingsResult>
{
    private readonly ICompanySettingsRepository _settingsRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaveCrmSettingsHandler> _logger;

    public SaveCrmSettingsHandler(
        ICompanySettingsRepository settingsRepo,
        IUnitOfWork unitOfWork,
        ILogger<SaveCrmSettingsHandler> logger)
    {
        _settingsRepo = settingsRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SaveCrmSettingsResult> Handle(
        SaveCrmSettingsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

#pragma warning disable CA1031 // Catch general exception — return structured error
        try
        {
            var settings = await _settingsRepo.GetByTenantIdAsync(request.TenantId, cancellationToken);

            if (settings is null)
            {
                settings = new CompanySettings
                {
                    TenantId = request.TenantId,
                    CompanyName = "Default",
                    AutoSyncStock = request.AutoAssignLeads
                };
                await _settingsRepo.AddAsync(settings, cancellationToken);
            }
            else
            {
                settings.AutoSyncStock = request.AutoAssignLeads;
                await _settingsRepo.UpdateAsync(settings, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "CRM ayarlari kaydedildi: Tenant={TenantId}, AutoAssign={AutoAssign}, Threshold={Threshold}",
                request.TenantId, request.AutoAssignLeads, request.LeadScoreThreshold);

            return SaveCrmSettingsResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRM ayarlari kaydetme hatasi: Tenant={TenantId}", request.TenantId);
            return SaveCrmSettingsResult.Failure(ex.Message);
        }
#pragma warning restore CA1031
    }
}
