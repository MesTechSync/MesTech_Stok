using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetJournalEntries;

public class GetJournalEntriesHandler : IRequestHandler<GetJournalEntriesQuery, IReadOnlyList<JournalEntryDto>>
{
    private readonly IJournalEntryRepository _repository;

    public GetJournalEntriesHandler(IJournalEntryRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<JournalEntryDto>> Handle(GetJournalEntriesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entries = await _repository.GetByDateRangeAsync(request.TenantId, request.From, request.To, cancellationToken);
        return entries.Select(e => new JournalEntryDto
        {
            Id = e.Id,
            EntryDate = e.EntryDate,
            Description = e.Description,
            ReferenceNumber = e.ReferenceNumber,
            IsPosted = e.IsPosted,
            PostedAt = e.PostedAt,
            TotalDebit = e.Lines.Sum(l => l.Debit),
            TotalCredit = e.Lines.Sum(l => l.Credit),
            Lines = e.Lines.Select(l => new JournalLineDto
            {
                Id = l.Id,
                AccountId = l.AccountId,
                AccountCode = l.Account?.Code,
                AccountName = l.Account?.Name,
                Debit = l.Debit,
                Credit = l.Credit,
                Description = l.Description
            }).ToList()
        }).ToList().AsReadOnly();
    }
}
