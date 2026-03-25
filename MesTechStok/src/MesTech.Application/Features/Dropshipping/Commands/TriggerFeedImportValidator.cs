using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands;

public sealed class TriggerFeedImportValidator : AbstractValidator<TriggerFeedImportCommand>
{
    public TriggerFeedImportValidator()
    {
        RuleFor(x => x.FeedId).NotEmpty();
    }
}
