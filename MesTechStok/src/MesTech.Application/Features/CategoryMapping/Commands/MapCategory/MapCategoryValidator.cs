using FluentValidation;

namespace MesTech.Application.Features.CategoryMapping.Commands.MapCategory;

public sealed class MapCategoryValidator : AbstractValidator<MapCategoryCommand>
{
    public MapCategoryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.InternalCategoryId).NotEmpty();
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.PlatformCategoryId).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PlatformCategoryName).NotEmpty().MaximumLength(500);
    }
}
