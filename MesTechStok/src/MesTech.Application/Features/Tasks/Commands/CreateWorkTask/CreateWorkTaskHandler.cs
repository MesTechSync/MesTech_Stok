using MediatR;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Commands.CreateWorkTask;

public sealed class CreateWorkTaskHandler : IRequestHandler<CreateWorkTaskCommand, Guid>
{
    private readonly IWorkTaskRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateWorkTaskHandler(IWorkTaskRepository repo, IUnitOfWork uow)
        => (_repository, _uow) = (repo, uow);

    public async Task<Guid> Handle(CreateWorkTaskCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var task = WorkTask.Create(request.TenantId, request.Title, request.Priority,
            request.ProjectId, request.MilestoneId, request.AssignedToUserId, request.CreatedByUserId,
            request.DueDate, request.EstimatedMinutes, request.OrderId, request.CrmContactId, request.ProductId);
        await _repository.AddAsync(task, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return task.Id;
    }
}
