using FluentValidation;

namespace MesTech.Application.Queries.GetCategoriesPaged;

public sealed class GetCategoriesPagedValidator : AbstractValidator<GetCategoriesPagedQuery>
{
    public GetCategoriesPagedValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SearchTerm).MaximumLength(200).When(x => x.SearchTerm != null);
    }
}
