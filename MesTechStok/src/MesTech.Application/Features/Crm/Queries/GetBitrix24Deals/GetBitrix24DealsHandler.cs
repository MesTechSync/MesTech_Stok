using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetBitrix24Deals;

public sealed class GetBitrix24DealsHandler
    : IRequestHandler<GetBitrix24DealsQuery, Bitrix24DealsResult>
{
    private readonly IDealRepository _dealRepo;

    public GetBitrix24DealsHandler(IDealRepository dealRepo)
        => _dealRepo = dealRepo ?? throw new ArgumentNullException(nameof(dealRepo));

    public async Task<Bitrix24DealsResult> Handle(
        GetBitrix24DealsQuery request, CancellationToken cancellationToken)
    {
        var allDeals = await _dealRepo.GetByTenantAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        var filtered = request.StageId.HasValue
            ? allDeals.Where(d => d.StageId == request.StageId.Value).ToList()
            : allDeals.ToList();

        var paged = filtered
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DealCardDto
            {
                DealId = d.Id,
                Title = d.Title,
                Value = d.Amount,
                Currency = d.Currency,
                ContactName = d.Contact?.FullName,
                StageId = d.StageId,
                StageName = d.Stage?.Name ?? "Unknown",
                Status = d.Status.ToString(),
                ExpectedCloseDate = d.ExpectedCloseDate,
                CreatedAt = d.CreatedAt
            })
            .ToList();

        return new Bitrix24DealsResult
        {
            Deals = paged,
            TotalCount = filtered.Count
        };
    }
}
