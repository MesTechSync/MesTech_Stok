using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetLeads;

public sealed class GetLeadsValidator : AbstractValidator<GetLeadsQuery>
{
    public GetLeadsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
