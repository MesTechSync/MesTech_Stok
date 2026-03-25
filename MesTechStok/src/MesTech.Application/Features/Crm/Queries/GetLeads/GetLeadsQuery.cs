using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Crm.Queries.GetLeads;

public record GetLeadsQuery(
    Guid TenantId,
    LeadStatus? Status = null,
    Guid? AssignedToUserId = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<GetLeadsResult>;

public sealed class GetLeadsResult
{
    public IReadOnlyList<LeadDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
