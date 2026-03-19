using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetLeads;

public class GetLeadsHandler : IRequestHandler<GetLeadsQuery, GetLeadsResult>
{
    private readonly ICrmLeadRepository _repository;

    public GetLeadsHandler(ICrmLeadRepository repository) => _repository = repository;

    public async Task<GetLeadsResult> Handle(GetLeadsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.TenantId, request.Status, request.AssignedToUserId,
            request.Page, request.PageSize, cancellationToken);

        return new GetLeadsResult
        {
            Items = items.Select(l => new LeadDto
            {
                Id = l.Id,
                FullName = l.FullName,
                Email = l.Email,
                Phone = l.Phone,
                Company = l.Company,
                Source = l.Source.ToString(),
                Status = l.Status.ToString(),
                AssignedToUserId = l.AssignedToUserId,
                ContactedAt = l.ContactedAt,
                CreatedAt = l.CreatedAt
            }).ToList().AsReadOnly(),
            TotalCount = totalCount
        };
    }
}
