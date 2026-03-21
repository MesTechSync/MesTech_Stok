using MediatR;
using MesTech.Domain.Entities.Crm;
namespace MesTech.Application.Features.Crm.Queries.GetCampaigns;
public record GetCampaignsQuery(Guid TenantId, bool? ActiveOnly = null) : IRequest<IReadOnlyList<Campaign>>;
