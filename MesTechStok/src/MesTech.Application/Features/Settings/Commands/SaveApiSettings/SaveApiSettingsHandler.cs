using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Settings.Commands.SaveApiSettings;

/// <summary>
/// API ayarlarini CompanySettings uzerinde kaydeder.
/// Tenant yoksa yeni kayit olusturur, varsa gunceller.
/// </summary>
public sealed class SaveApiSettingsHandler : IRequestHandler<SaveApiSettingsCommand, SaveApiSettingsResult>
{
    private readonly ICompanySettingsRepository _settingsRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaveApiSettingsHandler> _logger;

    public SaveApiSettingsHandler(
        ICompanySettingsRepository settingsRepo,
        IUnitOfWork unitOfWork,
        ILogger<SaveApiSettingsHandler> logger)
    {
        _settingsRepo = settingsRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SaveApiSettingsResult> Handle(
        SaveApiSettingsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

#pragma warning disable CA1031 // Catch general exception — return structured error
        try
        {
            var settings = await _settingsRepo.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

            if (settings is null)
            {
                settings = new CompanySettings
                {
                    TenantId = request.TenantId,
                    CompanyName = "Default"
                };
                await _settingsRepo.AddAsync(settings, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _settingsRepo.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "API ayarlari kaydedildi: Tenant={TenantId}, BaseUrl={BaseUrl}, RateLimit={RateLimit}/min",
                request.TenantId, request.ApiBaseUrl, request.RateLimitPerMinute);

            return SaveApiSettingsResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API ayarlari kaydetme hatasi: Tenant={TenantId}", request.TenantId);
            return SaveApiSettingsResult.Failure(ex.Message);
        }
#pragma warning restore CA1031
    }
}
