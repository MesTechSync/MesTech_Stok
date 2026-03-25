using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Settings.Commands.UpdateProfileSettings;

public sealed class UpdateProfileSettingsHandler : IRequestHandler<UpdateProfileSettingsCommand, bool>
{
    private readonly ITenantRepository _repo;
    public UpdateProfileSettingsHandler(ITenantRepository repo) => _repo = repo;

    public async Task<bool> Handle(UpdateProfileSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repo.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null) return false;

        tenant.Name = request.Name;
        tenant.TaxNumber = request.TaxNumber;
        await _repo.UpdateAsync(tenant, cancellationToken);
        return true;
    }
}
