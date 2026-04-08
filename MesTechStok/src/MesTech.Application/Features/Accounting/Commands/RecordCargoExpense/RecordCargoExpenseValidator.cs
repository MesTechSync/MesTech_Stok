using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;

public sealed class RecordCargoExpenseValidator : AbstractValidator<RecordCargoExpenseCommand>
{
    public RecordCargoExpenseValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CarrierName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Cost).GreaterThan(0).WithMessage("Kargo maliyeti sıfırdan büyük olmalıdır.");
        RuleFor(x => x.OrderId).MaximumLength(500).When(x => x.OrderId != null);
        RuleFor(x => x.TrackingNumber).MaximumLength(500).When(x => x.TrackingNumber != null);
    }
}
