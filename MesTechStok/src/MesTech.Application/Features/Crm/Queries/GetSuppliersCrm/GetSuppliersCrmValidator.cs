using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;

public sealed class GetSuppliersCrmValidator : AbstractValidator<GetSuppliersCrmQuery>
{
    public GetSuppliersCrmValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SearchTerm).MaximumLength(200).When(x => x.SearchTerm is not null);
    }
}
