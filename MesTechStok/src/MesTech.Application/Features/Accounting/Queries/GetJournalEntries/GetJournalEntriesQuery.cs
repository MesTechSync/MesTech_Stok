using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetJournalEntries;

public record GetJournalEntriesQuery(Guid TenantId, DateTime From, DateTime To)
    : IRequest<IReadOnlyList<JournalEntryDto>>;
