using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tenant.Commands.CreateTenant;

public sealed class CreateTenantHandler : IRequestHandler<CreateTenantCommand, Guid>
{
    private readonly ITenantRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateTenantHandler(ITenantRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = new Domain.Entities.Tenant
        {
            Name = request.Name,
            TaxNumber = request.TaxNumber,
            IsActive = true
        };
        await _repo.AddAsync(tenant, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tenant.Id;
    }
}
