using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;

public sealed class GetStockAlertsValidator : AbstractValidator<GetStockAlertsQuery>
{
    public GetStockAlertsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
