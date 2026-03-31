using MediatR;
using MesTech.Application.DTOs.Settings;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;

public sealed class GetCredentialsSettingsHandler : IRequestHandler<GetCredentialsSettingsQuery, CredentialsSettingsDto>
{
    private readonly IStoreRepository _storeRepo;
    public GetCredentialsSettingsHandler(IStoreRepository storeRepo) => _storeRepo = storeRepo;

    public async Task<CredentialsSettingsDto> Handle(GetCredentialsSettingsQuery request, CancellationToken cancellationToken)
    {
        var stores = await _storeRepo.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        var platforms = stores.Where(s => s.IsActive).Select(s => s.PlatformType.ToString()).Distinct().OrderBy(p => p).ToList();
        return new CredentialsSettingsDto(platforms);
    }
}
