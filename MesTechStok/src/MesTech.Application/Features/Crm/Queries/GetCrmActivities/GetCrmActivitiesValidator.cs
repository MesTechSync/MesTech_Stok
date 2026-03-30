using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetCrmActivities;

public sealed class GetCrmActivitiesValidator : AbstractValidator<GetCrmActivitiesQuery>
{
    public GetCrmActivitiesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
