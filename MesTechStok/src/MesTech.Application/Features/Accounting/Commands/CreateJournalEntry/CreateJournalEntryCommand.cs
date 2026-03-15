using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;

public record CreateJournalEntryCommand(
    Guid TenantId,
    DateTime EntryDate,
    string Description,
    string? ReferenceNumber,
    List<JournalLineInput> Lines
) : IRequest<Guid>;

public record JournalLineInput(
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string? Description
);
