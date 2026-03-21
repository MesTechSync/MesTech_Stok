using MediatR;

namespace MesTech.Application.Features.Billing.Queries.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery() : IRequest<IReadOnlyList<SubscriptionPlanDto>>;

public record SubscriptionPlanDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal AnnualPrice { get; init; }
    public string CurrencyCode { get; init; } = "TRY";
    public int MaxStores { get; init; }
    public int MaxProducts { get; init; }
    public int MaxUsers { get; init; }
    public int TrialDays { get; init; }
}
