using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tenant.Queries.GetTenants;

public sealed class GetTenantsHandler : IRequestHandler<GetTenantsQuery, GetTenantsResult>
{
    private readonly ITenantRepository _repo;
    public GetTenantsHandler(ITenantRepository repo) => _repo = repo;

    public async Task<GetTenantsResult> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var all = await _repo.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var totalCount = all.Count;

        var items = all
            .OrderBy(t => t.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                TaxNumber = t.TaxNumber,
                IsActive = t.IsActive,
                StoreCount = t.Stores.Count,
                UserCount = t.Users.Count,
                CreatedAt = t.CreatedAt
            })
            .ToList();

        return new GetTenantsResult(items, totalCount, request.Page, request.PageSize);
    }
}
