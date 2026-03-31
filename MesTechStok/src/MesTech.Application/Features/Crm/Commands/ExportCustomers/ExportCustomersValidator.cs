using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.ExportCustomers;

public sealed class ExportCustomersValidator : AbstractValidator<ExportCustomersCommand>
{
    private static readonly string[] AllowedFormats = ["XLSX", "CSV", "PDF"];

    public ExportCustomersValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(f => AllowedFormats.Contains(f.ToUpperInvariant()))
            .WithMessage("Format xlsx, csv veya pdf olmali.");
        RuleFor(x => x.SearchTerm)
            .MaximumLength(500)
            .When(x => x.SearchTerm is not null);
    }
}
