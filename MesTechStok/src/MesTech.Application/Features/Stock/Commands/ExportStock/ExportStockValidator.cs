using FluentValidation;

namespace MesTech.Application.Features.Stock.Commands.ExportStock;

public sealed class ExportStockValidator : AbstractValidator<ExportStockCommand>
{
    private static readonly string[] AllowedFormats = ["XLSX", "CSV", "PDF"];

    public ExportStockValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(f => AllowedFormats.Contains(f.ToUpperInvariant()))
            .WithMessage("Format xlsx, csv veya pdf olmali.");
        RuleFor(x => x.Filter)
            .MaximumLength(500)
            .When(x => x.Filter is not null);
    }
}
