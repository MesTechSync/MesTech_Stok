using FluentValidation;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;

public class GetFulfillmentDashboardValidator : AbstractValidator<GetFulfillmentDashboardQuery>
{
    public GetFulfillmentDashboardValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
