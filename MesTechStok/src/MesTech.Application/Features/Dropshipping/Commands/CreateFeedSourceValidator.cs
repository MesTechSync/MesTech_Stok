using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public class CreateFeedSourceValidator : AbstractValidator<CreateFeedSourceCommand>
{
    public CreateFeedSourceValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FeedUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PriceMarkupPercent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceMarkupFixed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetPlatforms).MaximumLength(500).When(x => x.TargetPlatforms != null);
    }
}
