using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.UpdateJournalEntry;

/// <summary>
/// Yevmiye kaydı güncelleme — RowVersion ile optimistic concurrency (G228-DEV6).
/// Sadece IsPosted=false olan kayıtlar güncellenebilir.
/// </summary>
public record UpdateJournalEntryCommand(
    Guid Id,
    Guid TenantId,
    DateTime EntryDate,
    string Description,
    string? ReferenceNumber,
    List<JournalLineInput> Lines,
    byte[]? RowVersion
) : IRequest<UpdateJournalEntryResult>;

public sealed class UpdateJournalEntryResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public byte[]? NewRowVersion { get; init; }
}
