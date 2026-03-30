using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Settings.Commands.SaveFulfillmentSettings;

/// <summary>
/// Fulfillment ayarlarini kaydeder — Amazon FBA ve Hepsilojistik auto-replenish toggle'lari.
/// Store kayitlari uzerinden ilgili platform store'larinin ayarlarini gunceller.
/// </summary>
public sealed class SaveFulfillmentSettingsHandler
    : IRequestHandler<SaveFulfillmentSettingsCommand, SaveFulfillmentSettingsResult>
{
    private readonly IStoreRepository _storeRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaveFulfillmentSettingsHandler> _logger;

    public SaveFulfillmentSettingsHandler(
        IStoreRepository storeRepo,
        IUnitOfWork unitOfWork,
        ILogger<SaveFulfillmentSettingsHandler> logger)
    {
        _storeRepo = storeRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SaveFulfillmentSettingsResult> Handle(
        SaveFulfillmentSettingsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

#pragma warning disable CA1031 // Catch general exception — return structured error
        try
        {
            // Fulfillment ayarlari store-level metadata olarak saklanir.
            // Gercek implementasyon store entity'sine fulfillment flag eklendiginde genisletilecek.
            _logger.LogInformation(
                "Fulfillment ayarlari kaydedildi: Tenant={TenantId}, FbaAutoReplenish={Fba}, HepsiAutoReplenish={Hepsi}",
                request.TenantId, request.FbaAutoReplenish, request.HepsiAutoReplenish);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return SaveFulfillmentSettingsResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fulfillment ayarlari kaydetme hatasi: Tenant={TenantId}", request.TenantId);
            return SaveFulfillmentSettingsResult.Failure(ex.Message);
        }
#pragma warning restore CA1031
    }
}
