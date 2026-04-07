using MediatR;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Commands.CreatePromissoryNote;

public sealed class CreatePromissoryNoteHandler : IRequestHandler<CreatePromissoryNoteCommand, Guid>
{
    private readonly IPromissoryNoteRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreatePromissoryNoteHandler(IPromissoryNoteRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreatePromissoryNoteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var note = PromissoryNote.Create(
            request.TenantId, request.NoteNumber, request.Amount,
            request.IssueDate, request.MaturityDate,
            request.Type, request.DebtorName);

        await _repository.AddAsync(note, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return note.Id;
    }
}
