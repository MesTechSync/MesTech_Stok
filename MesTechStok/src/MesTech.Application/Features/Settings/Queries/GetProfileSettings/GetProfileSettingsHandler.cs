using MediatR;
using MesTech.Application.DTOs.Settings;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Settings.Queries.GetProfileSettings;

public sealed class GetProfileSettingsHandler : IRequestHandler<GetProfileSettingsQuery, ProfileSettingsDto?>
{
    private readonly ITenantRepository _repo;
    public GetProfileSettingsHandler(ITenantRepository repo) => _repo = repo;

    public async Task<ProfileSettingsDto?> Handle(GetProfileSettingsQuery request, CancellationToken cancellationToken)
    {
        var t = await _repo.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        return t is null ? null : new ProfileSettingsDto(t.Name, t.TaxNumber, t.IsActive);
    }
}
