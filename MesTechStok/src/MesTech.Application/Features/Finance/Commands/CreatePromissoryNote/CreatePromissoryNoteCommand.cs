using MediatR;
using MesTech.Domain.Entities.Finance;

namespace MesTech.Application.Features.Finance.Commands.CreatePromissoryNote;

public record CreatePromissoryNoteCommand(
    Guid TenantId,
    string NoteNumber,
    decimal Amount,
    DateTime IssueDate,
    DateTime MaturityDate,
    NoteType Type,
    string DebtorName
) : IRequest<Guid>;
