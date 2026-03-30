using FluentValidation;

namespace MesTech.Application.Queries.GetSuppliersPaged;

public sealed class GetSuppliersPagedValidator : AbstractValidator<GetSuppliersPagedQuery>
{
    public GetSuppliersPagedValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SearchTerm).MaximumLength(200).When(x => x.SearchTerm != null);
    }
}
