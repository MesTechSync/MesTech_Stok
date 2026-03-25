using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.ImportSettlement;

public sealed class ImportSettlementValidator : AbstractValidator<ImportSettlementCommand>
{
    public ImportSettlementValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Platform).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TotalGross).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalCommission).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalNet).GreaterThanOrEqualTo(0);
    }
}
