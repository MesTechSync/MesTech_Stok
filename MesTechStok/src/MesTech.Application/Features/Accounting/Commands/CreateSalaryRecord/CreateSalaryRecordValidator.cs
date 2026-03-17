using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;

public class CreateSalaryRecordValidator : AbstractValidator<CreateSalaryRecordCommand>
{
    public CreateSalaryRecordValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EmployeeName)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Employee name is required and must be at most 200 characters.");
        RuleFor(x => x.GrossSalary)
            .GreaterThan(0)
            .WithMessage("Gross salary must be positive.");
        RuleFor(x => x.SGKEmployer)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SGK employer contribution must be non-negative.");
        RuleFor(x => x.SGKEmployee)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SGK employee contribution must be non-negative.");
        RuleFor(x => x.IncomeTax)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Income tax must be non-negative.");
        RuleFor(x => x.StampTax)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stamp tax must be non-negative.");
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Year must be between 2000 and 2100.");
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12.");
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes != null);
    }
}
