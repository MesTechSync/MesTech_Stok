using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.ReplyToMessage;

public class ReplyToMessageHandler : IRequestHandler<ReplyToMessageCommand, Unit>
{
    private readonly IPlatformMessageRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ReplyToMessageHandler(IPlatformMessageRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ReplyToMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _repository.GetByIdAsync(request.MessageId, cancellationToken)
            ?? throw new InvalidOperationException($"Message {request.MessageId} not found.");

        message.SetReply(request.Reply, request.RepliedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
