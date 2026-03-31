using MediatR;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;

public sealed class CreateJournalEntryHandler : IRequestHandler<CreateJournalEntryCommand, Guid>
{
    private readonly IJournalEntryRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateJournalEntryHandler(IJournalEntryRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = JournalEntry.Create(request.TenantId, request.EntryDate, request.Description, request.ReferenceNumber);

        foreach (var line in request.Lines)
        {
            entry.AddLine(line.AccountId, line.Debit, line.Credit, line.Description);
        }

        entry.Validate();

        await _repository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Id;
    }
}
