using FluentValidation;

namespace MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;

public class FetchProductFromUrlValidator : AbstractValidator<FetchProductFromUrlCommand>
{
    public FetchProductFromUrlValidator()
    {
        RuleFor(x => x.ProductUrl).NotEmpty().MaximumLength(500);
    }
}
