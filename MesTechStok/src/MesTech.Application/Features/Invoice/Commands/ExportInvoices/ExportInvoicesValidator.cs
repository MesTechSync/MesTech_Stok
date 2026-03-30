using FluentValidation;

namespace MesTech.Application.Features.Invoice.Commands.ExportInvoices;

public sealed class ExportInvoicesValidator : AbstractValidator<ExportInvoicesCommand>
{
    private static readonly string[] AllowedFormats = ["XLSX", "CSV", "PDF"];

    public ExportInvoicesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(f => AllowedFormats.Contains(f.ToUpperInvariant()))
            .WithMessage("Format xlsx, csv veya pdf olmali.");
        RuleFor(x => x.DateFrom)
            .LessThan(x => x.DateTo)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("Baslangic tarihi bitis tarihinden once olmali.");
    }
}
