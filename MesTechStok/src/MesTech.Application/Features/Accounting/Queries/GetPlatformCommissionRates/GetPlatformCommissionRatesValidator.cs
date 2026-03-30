using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;

public sealed class GetPlatformCommissionRatesValidator : AbstractValidator<GetPlatformCommissionRatesQuery>
{
    public GetPlatformCommissionRatesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
