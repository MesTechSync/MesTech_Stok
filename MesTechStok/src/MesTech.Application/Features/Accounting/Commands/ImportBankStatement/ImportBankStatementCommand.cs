using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.ImportBankStatement;

public record ImportBankStatementCommand(
    Guid TenantId,
    Guid BankAccountId,
    List<BankTransactionInput> Transactions
) : IRequest<int>;

public record BankTransactionInput(
    DateTime TransactionDate,
    decimal Amount,
    string Description,
    string? ReferenceNumber = null,
    string? IdempotencyKey = null
);
