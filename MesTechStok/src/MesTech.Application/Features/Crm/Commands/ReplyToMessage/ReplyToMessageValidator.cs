using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.ReplyToMessage;

public sealed class ReplyToMessageValidator : AbstractValidator<ReplyToMessageCommand>
{
    public ReplyToMessageValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Reply).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RepliedBy).NotEmpty().MaximumLength(500);
    }
}
