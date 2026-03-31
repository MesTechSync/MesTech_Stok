using MediatR;
using MesTech.Application.DTOs.Settings;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Settings.Queries.GetGeneralSettings;

public sealed class GetGeneralSettingsHandler : IRequestHandler<GetGeneralSettingsQuery, GeneralSettingsDto?>
{
    private readonly ITenantRepository _repo;
    public GetGeneralSettingsHandler(ITenantRepository repo) => _repo = repo;

    public async Task<GeneralSettingsDto?> Handle(GetGeneralSettingsQuery request, CancellationToken cancellationToken)
    {
        var t = await _repo.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        return t is null ? null : new GeneralSettingsDto(t.Name, "TRY", "tr-TR", "Europe/Istanbul");
    }
}
