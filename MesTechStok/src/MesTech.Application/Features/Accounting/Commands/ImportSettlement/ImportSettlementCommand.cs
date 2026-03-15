using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.ImportSettlement;

public record ImportSettlementCommand(
    Guid TenantId,
    string Platform,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal TotalGross,
    decimal TotalCommission,
    decimal TotalNet,
    List<SettlementLineInput> Lines
) : IRequest<Guid>;

public record SettlementLineInput(
    string? OrderId,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal ServiceFee,
    decimal CargoDeduction,
    decimal RefundDeduction,
    decimal NetAmount
);
