using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.RecordCommission;

public record RecordCommissionCommand(
    Guid TenantId,
    string Platform,
    decimal GrossAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal ServiceFee,
    string? OrderId = null,
    string? Category = null
) : IRequest<Guid>;
