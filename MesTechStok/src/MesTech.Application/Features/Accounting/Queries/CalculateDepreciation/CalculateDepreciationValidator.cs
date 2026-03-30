using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;

public sealed class CalculateDepreciationValidator : AbstractValidator<CalculateDepreciationQuery>
{
    public CalculateDepreciationValidator()
    {
        RuleFor(x => x.AssetId).NotEqual(Guid.Empty);
    }
}
