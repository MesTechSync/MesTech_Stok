using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Accounting.Commands.UpdateSalaryRecord;

public record UpdateSalaryRecordCommand(
    Guid Id,
    PaymentStatus PaymentStatus,
    DateTime? PaidDate = null
) : IRequest;
