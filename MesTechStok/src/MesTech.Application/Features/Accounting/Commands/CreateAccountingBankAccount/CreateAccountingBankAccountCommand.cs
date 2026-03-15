using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.CreateAccountingBankAccount;

public record CreateAccountingBankAccountCommand(
    Guid TenantId,
    string AccountName,
    string Currency = "TRY",
    string? BankName = null,
    string? IBAN = null,
    string? AccountNumber = null,
    bool IsDefault = false,
    Guid? StoreId = null
) : IRequest<Guid>;
