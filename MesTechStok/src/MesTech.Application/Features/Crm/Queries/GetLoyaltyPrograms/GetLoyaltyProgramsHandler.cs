using MediatR;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
namespace MesTech.Application.Features.Crm.Queries.GetLoyaltyPrograms;
public class GetLoyaltyProgramsHandler : IRequestHandler<GetLoyaltyProgramsQuery, IReadOnlyList<LoyaltyProgram>>
{
    private readonly ILoyaltyRepository _repo;
    public GetLoyaltyProgramsHandler(ILoyaltyRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<LoyaltyProgram>> Handle(GetLoyaltyProgramsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _repo.GetByTenantAsync(request.TenantId, cancellationToken);
    }
}
