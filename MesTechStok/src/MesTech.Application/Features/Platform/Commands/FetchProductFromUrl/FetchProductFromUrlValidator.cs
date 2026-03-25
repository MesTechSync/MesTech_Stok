using FluentValidation;

namespace MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;

public sealed class FetchProductFromUrlValidator : AbstractValidator<FetchProductFromUrlCommand>
{
    public FetchProductFromUrlValidator()
    {
        RuleFor(x => x.ProductUrl).NotEmpty().MaximumLength(500);
    }
}
