using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.ParseAndImportSettlement;

public sealed class ParseAndImportSettlementValidator : AbstractValidator<ParseAndImportSettlementCommand>
{
    private static readonly string[] SupportedFormats = ["JSON", "TSV", "CSV", "XML"];

    public ParseAndImportSettlementValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Platform).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RawData).NotEmpty()
            .Must(data => data.Length > 0)
            .WithMessage("Settlement file data cannot be empty.");
        RuleFor(x => x.Format).NotEmpty()
            .Must(f => SupportedFormats.Contains(f, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Format must be one of: {string.Join(", ", SupportedFormats)}");
    }
}
