using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Accounting.Commands.UpdatePenaltyRecord;

public record UpdatePenaltyRecordCommand(
    Guid Id,
    PaymentStatus PaymentStatus
) : IRequest;
