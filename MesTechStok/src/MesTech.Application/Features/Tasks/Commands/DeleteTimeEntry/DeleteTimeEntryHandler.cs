using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Commands.DeleteTimeEntry;

public sealed class DeleteTimeEntryHandler : IRequestHandler<DeleteTimeEntryCommand, DeleteTimeEntryResult>
{
    private readonly ITimeEntryRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteTimeEntryHandler(ITimeEntryRepository repository, IUnitOfWork uow)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<DeleteTimeEntryResult> Handle(DeleteTimeEntryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new DeleteTimeEntryResult(false, $"Zaman kaydı {request.Id} bulunamadı.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeleteTimeEntryResult(true);
    }
}
