using FluentValidation;

namespace MesTech.Application.Features.Invoice.Commands.ExportInvoiceReport;

public sealed class ExportInvoiceReportValidator : AbstractValidator<ExportInvoiceReportCommand>
{
    private static readonly string[] AllowedFormats = ["XLSX", "CSV", "PDF"];

    public ExportInvoiceReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(f => AllowedFormats.Contains(f.ToUpperInvariant()))
            .WithMessage("Format xlsx, csv veya pdf olmali.");
        RuleFor(x => x.Period)
            .MaximumLength(100)
            .When(x => x.Period is not null);
        RuleFor(x => x.DateFrom)
            .LessThan(x => x.DateTo)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("Baslangic tarihi bitis tarihinden once olmali.");
    }
}
