using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tenant.Commands.UpdateTenant;

public sealed class UpdateTenantHandler : IRequestHandler<UpdateTenantCommand, bool>
{
    private readonly ITenantRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateTenantHandler(ITenantRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repo.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null) return false;
        tenant.Name = request.Name;
        tenant.TaxNumber = request.TaxNumber;
        tenant.IsActive = request.IsActive;
        await _repo.UpdateAsync(tenant, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
