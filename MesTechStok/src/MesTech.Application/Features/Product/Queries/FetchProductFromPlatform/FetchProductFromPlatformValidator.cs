using FluentValidation;

namespace MesTech.Application.Features.Product.Queries.FetchProductFromPlatform;

public sealed class FetchProductFromPlatformValidator
    : AbstractValidator<FetchProductFromPlatformQuery>
{
    public FetchProductFromPlatformValidator()
    {
        RuleFor(x => x.ProductUrl)
            .NotEmpty().WithMessage("Product URL is required")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == "https" || uri.Scheme == "http"))
            .WithMessage("Valid HTTP/HTTPS URL is required");
    }
}
