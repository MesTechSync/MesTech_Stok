using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Settings.Queries.GetStoreSettings;

public sealed class GetStoreSettingsHandler : IRequestHandler<GetStoreSettingsQuery, StoreSettingsDto>
{
    private readonly ICompanySettingsRepository _settingsRepo;
    private readonly IStoreRepository _storeRepo;

    public GetStoreSettingsHandler(ICompanySettingsRepository settingsRepo, IStoreRepository storeRepo)
    {
        _settingsRepo = settingsRepo;
        _storeRepo = storeRepo;
    }

    public async Task<StoreSettingsDto> Handle(GetStoreSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepo.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        var stores = await _storeRepo.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        return new StoreSettingsDto
        {
            CompanyName = settings?.CompanyName ?? string.Empty,
            TaxNumber = settings?.TaxNumber,
            Phone = settings?.Phone,
            Email = settings?.Email,
            Address = settings?.Address,
            Stores = stores.Select(s => new StoreInfoDto
            {
                StoreId = s.Id,
                PlatformType = s.PlatformType.ToString(),
                StoreName = s.StoreName,
                IsActive = s.IsActive,
                HasCredentials = s.Credentials.Count > 0
            }).ToList()
        };
    }
}
