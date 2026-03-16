using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;

public class CreateChartOfAccountValidator : AbstractValidator<CreateChartOfAccountCommand>
{
    public CreateChartOfAccountValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^[0-9.]+$")
            .WithMessage("Account code must contain only digits and dots (e.g., '100', '100.01').");
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.AccountType).IsInEnum();
        RuleFor(x => x.Level)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(5)
            .WithMessage("Account level must be between 1 and 5.");
    }
}
