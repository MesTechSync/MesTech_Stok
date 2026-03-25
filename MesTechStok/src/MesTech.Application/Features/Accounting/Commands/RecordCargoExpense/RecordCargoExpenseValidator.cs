using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;

public sealed class RecordCargoExpenseValidator : AbstractValidator<RecordCargoExpenseCommand>
{
    public RecordCargoExpenseValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CarrierName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OrderId).MaximumLength(500).When(x => x.OrderId != null);
        RuleFor(x => x.TrackingNumber).MaximumLength(500).When(x => x.TrackingNumber != null);
    }
}
