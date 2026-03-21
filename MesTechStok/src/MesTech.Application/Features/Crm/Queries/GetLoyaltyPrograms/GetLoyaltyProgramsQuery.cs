using MediatR;
using MesTech.Domain.Entities.Crm;
namespace MesTech.Application.Features.Crm.Queries.GetLoyaltyPrograms;
public record GetLoyaltyProgramsQuery(Guid TenantId) : IRequest<IReadOnlyList<LoyaltyProgram>>;
