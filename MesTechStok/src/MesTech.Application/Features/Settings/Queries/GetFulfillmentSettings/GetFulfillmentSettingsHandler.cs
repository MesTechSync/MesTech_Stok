using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Settings.Queries.GetFulfillmentSettings;

public sealed class GetFulfillmentSettingsHandler
    : IRequestHandler<GetFulfillmentSettingsQuery, FulfillmentSettingsDto>
{
    private readonly IStoreRepository _storeRepo;

    public GetFulfillmentSettingsHandler(IStoreRepository storeRepo)
        => _storeRepo = storeRepo ?? throw new ArgumentNullException(nameof(storeRepo));

    public async Task<FulfillmentSettingsDto> Handle(
        GetFulfillmentSettingsQuery request, CancellationToken cancellationToken)
    {
        var stores = await _storeRepo.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var fbaStore = stores.FirstOrDefault(s => s.PlatformType == PlatformType.Amazon && s.IsActive);
        var hepsiStore = stores.FirstOrDefault(s => s.PlatformType == PlatformType.Hepsiburada && s.IsActive);

        return new FulfillmentSettingsDto
        {
            AmazonFba = fbaStore is not null
                ? new FulfillmentProviderDto
                {
                    IsConfigured = true,
                    AutoReplenish = false,
                    ConnectionStatus = "Baglanti aktif",
                    LastSyncAt = fbaStore.LastSettlementDate
                }
                : new FulfillmentProviderDto(),
            Hepsilojistik = hepsiStore is not null
                ? new FulfillmentProviderDto
                {
                    IsConfigured = true,
                    AutoReplenish = false,
                    ConnectionStatus = "Baglanti aktif",
                    LastSyncAt = hepsiStore.LastSettlementDate
                }
                : new FulfillmentProviderDto()
        };
    }
}
