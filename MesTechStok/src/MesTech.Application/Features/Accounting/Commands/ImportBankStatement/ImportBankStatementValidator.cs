using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.ImportBankStatement;

public class ImportBankStatementValidator : AbstractValidator<ImportBankStatementCommand>
{
    public ImportBankStatementValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.BankAccountId).NotEmpty();
    }
}
