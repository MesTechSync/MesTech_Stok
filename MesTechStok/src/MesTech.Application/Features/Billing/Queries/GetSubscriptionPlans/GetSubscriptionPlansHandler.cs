using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Billing.Queries.GetSubscriptionPlans;

public sealed class GetSubscriptionPlansHandler : IRequestHandler<GetSubscriptionPlansQuery, IReadOnlyList<SubscriptionPlanDto>>
{
    private readonly ISubscriptionPlanRepository _repository;

    public GetSubscriptionPlansHandler(ISubscriptionPlanRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<SubscriptionPlanDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plans = await _repository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        return plans.Select(p => new SubscriptionPlanDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            MonthlyPrice = p.MonthlyPrice,
            AnnualPrice = p.AnnualPrice,
            CurrencyCode = p.CurrencyCode,
            MaxStores = p.MaxStores,
            MaxProducts = p.MaxProducts,
            MaxUsers = p.MaxUsers,
            TrialDays = p.TrialDays
        }).ToList().AsReadOnly();
    }
}
