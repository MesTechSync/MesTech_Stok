using FluentValidation;

namespace MesTech.Application.Features.CategoryMapping.Queries.GetCategoryMappings;

public sealed class GetCategoryMappingsValidator : AbstractValidator<GetCategoryMappingsQuery>
{
    public GetCategoryMappingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
