using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands.PreviewFeed;

public sealed class PreviewFeedValidator : AbstractValidator<PreviewFeedCommand>
{
    public PreviewFeedValidator()
    {
        RuleFor(x => x.FeedSourceId).NotEmpty();
    }
}
