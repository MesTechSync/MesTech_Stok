using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Commands.DeleteWorkTask;

public sealed class DeleteWorkTaskHandler : IRequestHandler<DeleteWorkTaskCommand, DeleteWorkTaskResult>
{
    private readonly IWorkTaskRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteWorkTaskHandler(IWorkTaskRepository repository, IUnitOfWork uow)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<DeleteWorkTaskResult> Handle(DeleteWorkTaskCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new DeleteWorkTaskResult(false, $"Görev {request.Id} bulunamadı.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeleteWorkTaskResult(true);
    }
}
