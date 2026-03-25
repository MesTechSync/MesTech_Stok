using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Commands.CompleteTask;

public sealed class CompleteTaskHandler : IRequestHandler<CompleteTaskCommand, Unit>
{
    private readonly IWorkTaskRepository _tasks;
    private readonly IUnitOfWork _uow;

    public CompleteTaskHandler(IWorkTaskRepository tasks, IUnitOfWork uow)
        => (_tasks, _uow) = (tasks, uow);

    public async Task<Unit> Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new InvalidOperationException($"WorkTask {request.TaskId} not found.");

        task.Complete(request.UserId);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
