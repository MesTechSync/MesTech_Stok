using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Commands.CompleteTask;

public class CompleteTaskHandler : IRequestHandler<CompleteTaskCommand, Unit>
{
    private readonly IWorkTaskRepository _tasks;
    private readonly IUnitOfWork _uow;

    public CompleteTaskHandler(IWorkTaskRepository tasks, IUnitOfWork uow)
        => (_tasks, _uow) = (tasks, uow);

    public async Task<Unit> Handle(CompleteTaskCommand req, CancellationToken ct)
    {
        var task = await _tasks.GetByIdAsync(req.TaskId, ct)
            ?? throw new InvalidOperationException($"WorkTask {req.TaskId} not found.");

        task.Complete(req.UserId);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
