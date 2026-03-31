using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UpdateJournalEntry;

/// <summary>
/// Yevmiye kaydı güncelleme handler — RowVersion optimistic concurrency (G228-DEV6).
/// </summary>
public sealed class UpdateJournalEntryHandler : IRequestHandler<UpdateJournalEntryCommand, UpdateJournalEntryResult>
{
    private readonly IJournalEntryRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateJournalEntryHandler(IJournalEntryRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<UpdateJournalEntryResult> Handle(
        UpdateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return new UpdateJournalEntryResult { IsSuccess = false, ErrorMessage = "Journal entry not found." };

        if (entry.TenantId != request.TenantId)
            return new UpdateJournalEntryResult { IsSuccess = false, ErrorMessage = "Tenant mismatch." };

        if (entry.IsPosted)
            return new UpdateJournalEntryResult { IsSuccess = false, ErrorMessage = "Cannot modify a posted journal entry." };

        // Optimistic concurrency check
        if (request.RowVersion is not null && entry.RowVersion is not null
            && !request.RowVersion.SequenceEqual(entry.RowVersion))
        {
            return new UpdateJournalEntryResult
            {
                IsSuccess = false,
                ErrorMessage = "Concurrency conflict — record was modified by another user."
            };
        }

        entry.Update(request.EntryDate, request.Description, request.ReferenceNumber);

        foreach (var line in request.Lines)
        {
            entry.AddLine(line.AccountId, line.Debit, line.Credit, line.Description);
        }

        entry.Validate();

        await _repository.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new UpdateJournalEntryResult
        {
            IsSuccess = true,
            NewRowVersion = entry.RowVersion
        };
    }
}
