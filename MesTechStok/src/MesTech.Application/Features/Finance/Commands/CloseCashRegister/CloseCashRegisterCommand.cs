using MediatR;

namespace MesTech.Application.Features.Finance.Commands.CloseCashRegister;

/// <summary>
/// Gun sonu kasa kapama komutu.
/// Fiziksel sayim (ActualCashAmount) ile beklenen bakiye karsilastirilir.
/// </summary>
public record CloseCashRegisterCommand(
    Guid TenantId,
    Guid CashRegisterId,
    DateTime ClosingDate,
    decimal ActualCashAmount
) : IRequest<CloseCashRegisterResult>;

public record CloseCashRegisterResult(
    Guid CashRegisterId,
    DateTime ClosingDate,
    decimal ExpectedBalance,
    decimal ActualBalance,
    decimal CashDifference,
    int TransactionCount,
    bool IsClosed);
