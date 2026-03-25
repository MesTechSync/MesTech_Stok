using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands.ImportFromFeed;

public sealed class ImportFromFeedValidator : AbstractValidator<ImportFromFeedCommand>
{
    public ImportFromFeedValidator()
    {
        RuleFor(x => x.FeedSourceId).NotEmpty();
        RuleFor(x => x.PriceMultiplier).GreaterThanOrEqualTo(0);
    }
}
