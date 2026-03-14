using MediatR;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Commands.CreateWorkTask;

public class CreateWorkTaskHandler : IRequestHandler<CreateWorkTaskCommand, Guid>
{
    private readonly IWorkTaskRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateWorkTaskHandler(IWorkTaskRepository repo, IUnitOfWork uow)
        => (_repository, _uow) = (repo, uow);

    public async Task<Guid> Handle(CreateWorkTaskCommand req, CancellationToken ct)
    {
        var task = WorkTask.Create(req.TenantId, req.Title, req.Priority,
            req.ProjectId, req.MilestoneId, req.AssignedToUserId, req.CreatedByUserId,
            req.DueDate, req.EstimatedMinutes, req.OrderId, req.CrmContactId, req.ProductId);
        await _repository.AddAsync(task, ct);
        await _uow.SaveChangesAsync(ct);
        return task.Id;
    }
}
