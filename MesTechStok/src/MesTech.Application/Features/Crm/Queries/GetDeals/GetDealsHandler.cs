using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetDeals;

public class GetDealsHandler : IRequestHandler<GetDealsQuery, GetDealsResult>
{
    private readonly ICrmDealRepository _repository;
    public GetDealsHandler(ICrmDealRepository repository) => _repository = repository;

    public async Task<GetDealsResult> Handle(GetDealsQuery req, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(req);
        var deals = req.PipelineId.HasValue
            ? await _repository.GetByPipelineAsync(req.TenantId, req.PipelineId.Value, req.Status, cancellationToken)
            : await _repository.GetByTenantPagedAsync(req.TenantId, req.Status, req.Page, req.PageSize, cancellationToken);

        return new GetDealsResult
        {
            Items = deals.Select(d => new DealDto
            {
                Id = d.Id, Title = d.Title, Amount = d.Amount, Currency = d.Currency,
                Status = d.Status.ToString(), StageId = d.StageId,
                StageName = d.Stage?.Name ?? string.Empty, StageColor = d.Stage?.Color,
                CrmContactId = d.CrmContactId, ContactName = d.Contact?.FullName,
                ExpectedCloseDate = d.ExpectedCloseDate,
                AssignedToUserId = d.AssignedToUserId, CreatedAt = d.CreatedAt
            }).ToList().AsReadOnly(),
            TotalCount = deals.Count
        };
    }
}
