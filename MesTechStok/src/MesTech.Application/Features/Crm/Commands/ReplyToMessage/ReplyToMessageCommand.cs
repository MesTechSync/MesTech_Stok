using MediatR;

namespace MesTech.Application.Features.Crm.Commands.ReplyToMessage;

public record ReplyToMessageCommand(
    Guid MessageId,
    string Reply,
    string RepliedBy
) : IRequest<Unit>;
