using FluentValidation;

namespace MesTech.Application.Features.Finance.Commands.CreateExpense;

public sealed class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Gider tutarı sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes != null);
    }
}
