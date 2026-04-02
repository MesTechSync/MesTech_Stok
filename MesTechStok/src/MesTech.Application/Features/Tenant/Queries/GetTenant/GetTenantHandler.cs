using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tenant.Queries.GetTenant;

public sealed class GetTenantHandler : IRequestHandler<GetTenantQuery, TenantDto?>
{
    private readonly ITenantRepository _repo;
    public GetTenantHandler(ITenantRepository repo) => _repo = repo;

    public async Task<TenantDto?> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var t = await _repo.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        return t is null ? null : new TenantDto(t.Id, t.Name, t.TaxNumber, t.IsActive);
    }
}
