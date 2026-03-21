using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateAccountingBankAccount;

public class CreateAccountingBankAccountValidator : AbstractValidator<CreateAccountingBankAccountCommand>
{
    public CreateAccountingBankAccountValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BankName).MaximumLength(500).When(x => x.BankName != null);
        RuleFor(x => x.IBAN).MaximumLength(500).When(x => x.IBAN != null);
        RuleFor(x => x.AccountNumber).MaximumLength(500).When(x => x.AccountNumber != null);
    }
}
