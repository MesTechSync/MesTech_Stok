using FluentValidation;

namespace MesTech.Application.Features.Finance.Commands.RecordCashTransaction;

public sealed class RecordCashTransactionValidator : AbstractValidator<RecordCashTransactionCommand>
{
    public RecordCashTransactionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CashRegisterId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Tutar pozitif olmali.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category != null);
        RuleFor(x => x.Type).IsInEnum();
    }
}
