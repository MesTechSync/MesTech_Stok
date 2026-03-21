using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.ReplyToMessage;

public class ReplyToMessageValidator : AbstractValidator<ReplyToMessageCommand>
{
    public ReplyToMessageValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Reply).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RepliedBy).NotEmpty().MaximumLength(500);
    }
}
