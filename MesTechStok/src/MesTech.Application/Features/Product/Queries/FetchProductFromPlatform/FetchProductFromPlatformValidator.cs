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
                         && (string.Equals(uri.Scheme, "https", StringComparison.Ordinal) || string.Equals(uri.Scheme, "http", StringComparison.Ordinal)))
            .WithMessage("Valid HTTP/HTTPS URL is required");
    }
}
