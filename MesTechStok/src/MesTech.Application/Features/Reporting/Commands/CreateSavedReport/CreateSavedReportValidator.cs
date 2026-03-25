using FluentValidation;

namespace MesTech.Application.Features.Reporting.Commands.CreateSavedReport;

public sealed class CreateSavedReportValidator : AbstractValidator<CreateSavedReportCommand>
{
    public CreateSavedReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReportType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FilterJson).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.CreatedByUserId).NotEmpty();
    }
}
