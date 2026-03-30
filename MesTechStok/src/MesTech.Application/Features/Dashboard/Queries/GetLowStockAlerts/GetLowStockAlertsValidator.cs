using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetLowStockAlerts;

public sealed class GetLowStockAlertsValidator : AbstractValidator<GetLowStockAlertsQuery>
{
    public GetLowStockAlertsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
    }
}
