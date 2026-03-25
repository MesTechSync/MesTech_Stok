using FluentValidation;

namespace MesTech.Application.Commands.MapProductToPlatform;

public sealed class MapProductToPlatformValidator : AbstractValidator<MapProductToPlatformCommand>
{
    public MapProductToPlatformValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.PlatformCategoryId).NotEmpty().MaximumLength(500);
    }
}
