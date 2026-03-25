using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;

public sealed class CreateFixedExpenseValidator : AbstractValidator<CreateFixedExpenseCommand>
{
    public CreateFixedExpenseValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Name is required and must be at most 200 characters.");
        RuleFor(x => x.MonthlyAmount)
            .GreaterThan(0)
            .WithMessage("Monthly amount must be positive.");
        RuleFor(x => x.DayOfMonth)
            .InclusiveBetween(1, 31)
            .WithMessage("Day of month must be between 1 and 31.");
        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(3)
            .WithMessage("Currency code must be at most 3 characters.");
        RuleFor(x => x.SupplierName)
            .MaximumLength(200)
            .When(x => x.SupplierName != null);
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes != null);
        RuleFor(x => x)
            .Must(x => !x.EndDate.HasValue || x.EndDate.Value >= x.StartDate)
            .WithMessage("End date must be after start date.");
    }
}
