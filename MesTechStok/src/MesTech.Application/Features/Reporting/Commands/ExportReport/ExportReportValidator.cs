using FluentValidation;

namespace MesTech.Application.Features.Reporting.Commands.ExportReport;

public sealed class ExportReportValidator : AbstractValidator<ExportReportCommand>
{
    private static readonly string[] AllowedFormats = ["XLSX", "CSV", "PDF"];

    public ExportReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ReportType)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(f => AllowedFormats.Contains(f.ToUpperInvariant()))
            .WithMessage("Format xlsx, csv veya pdf olmali.");
    }
}
