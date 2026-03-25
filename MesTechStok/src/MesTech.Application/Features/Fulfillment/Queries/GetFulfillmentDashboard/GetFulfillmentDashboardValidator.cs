using FluentValidation;

namespace MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;

public sealed class GetFulfillmentDashboardValidator : AbstractValidator<GetFulfillmentDashboardQuery>
{
    public GetFulfillmentDashboardValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
