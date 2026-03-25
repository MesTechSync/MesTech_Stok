using FluentValidation;

namespace MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;

public sealed class DeleteSavedReportValidator : AbstractValidator<DeleteSavedReportCommand>
{
    public DeleteSavedReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ReportId).NotEmpty();
    }
}
