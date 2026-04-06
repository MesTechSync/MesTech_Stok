using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Commands.DeleteProject;

public sealed class DeleteProjectHandler : IRequestHandler<DeleteProjectCommand, DeleteProjectResult>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteProjectHandler(IProjectRepository repository, IUnitOfWork uow)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<DeleteProjectResult> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new DeleteProjectResult(false, $"Proje {request.Id} bulunamadı.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeleteProjectResult(true);
    }
}
