using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetCargoComparison;

public sealed class GetCargoComparisonValidator : AbstractValidator<GetCargoComparisonQuery>
{
    public GetCargoComparisonValidator()
    {
        RuleFor(x => x.ShipmentRequest).NotNull();
    }
}
