using MediatR;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;

public record CreatePenaltyRecordCommand(
    Guid TenantId,
    PenaltySource Source,
    string Description,
    decimal Amount,
    DateTime PenaltyDate,
    DateTime? DueDate = null,
    string? ReferenceNumber = null,
    Guid? RelatedOrderId = null,
    string Currency = "TRY",
    string? Notes = null
) : IRequest<Guid>;
