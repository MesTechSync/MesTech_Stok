using FluentValidation;

namespace MesTech.Application.Queries.GetCategories;

public sealed class GetCategoriesValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesValidator() { }
}
