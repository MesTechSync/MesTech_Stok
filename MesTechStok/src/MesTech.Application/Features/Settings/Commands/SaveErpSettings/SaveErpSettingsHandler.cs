using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Settings.Commands.SaveErpSettings;

/// <summary>
/// ERP ayarlarini CompanySettings uzerinde kaydeder.
/// Tenant yoksa yeni kayit olusturur, varsa gunceller.
/// </summary>
public sealed class SaveErpSettingsHandler : IRequestHandler<SaveErpSettingsCommand, SaveErpSettingsResult>
{
    private readonly ICompanySettingsRepository _settingsRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaveErpSettingsHandler> _logger;

    public SaveErpSettingsHandler(
        ICompanySettingsRepository settingsRepo,
        IUnitOfWork unitOfWork,
        ILogger<SaveErpSettingsHandler> logger)
    {
        _settingsRepo = settingsRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SaveErpSettingsResult> Handle(
        SaveErpSettingsCommand request, CancellationToken cancellationToken)
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
                    CompanyName = "Default",
                    ErpProvider = request.ErpProvider,
                    AutoSyncStock = request.AutoSyncStock,
                    AutoSyncInvoice = request.AutoSyncInvoice,
                    StockSyncPeriodMinutes = request.StockSyncPeriodMinutes,
                    PriceSyncPeriodMinutes = request.PriceSyncPeriodMinutes
                };
                await _settingsRepo.AddAsync(settings, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                settings.ErpProvider = request.ErpProvider;
                settings.AutoSyncStock = request.AutoSyncStock;
                settings.AutoSyncInvoice = request.AutoSyncInvoice;
                settings.StockSyncPeriodMinutes = request.StockSyncPeriodMinutes;
                settings.PriceSyncPeriodMinutes = request.PriceSyncPeriodMinutes;
                await _settingsRepo.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "ERP ayarlari kaydedildi: Tenant={TenantId}, Provider={Provider}, AutoStock={AutoStock}",
                request.TenantId, request.ErpProvider, request.AutoSyncStock);

            return SaveErpSettingsResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERP ayarlari kaydetme hatasi: Tenant={TenantId}", request.TenantId);
            return SaveErpSettingsResult.Failure(ex.Message);
        }
#pragma warning restore CA1031
    }
}
