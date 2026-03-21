using MediatR;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateAccountingBankAccount;

/// <summary>
/// Mevcut Finance.BankAccount entity'sini kullanarak banka hesabi olusturur.
/// </summary>
public class CreateAccountingBankAccountHandler : IRequestHandler<CreateAccountingBankAccountCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateAccountingBankAccountHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateAccountingBankAccountCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var bankAccount = BankAccount.Create(
            request.TenantId, request.AccountName, request.Currency,
            request.BankName, request.IBAN, request.AccountNumber, request.IsDefault, request.StoreId);

        // BankAccount DbSet — UnitOfWork SaveChanges ile kayit yapilir.
        await _uow.SaveChangesAsync(cancellationToken);
        return bankAccount.Id;
    }
}
