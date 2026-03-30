using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetExpenseReport;

public sealed class GetExpenseReportValidator : AbstractValidator<GetExpenseReportQuery>
{
    public GetExpenseReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.From).NotEmpty();
        RuleFor(x => x.To)
            .NotEmpty()
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("To date must be on or after From date.");
        RuleFor(x => x.CategoryFilter)
            .MaximumLength(100)
            .When(x => x.CategoryFilter != null);
    }
}
