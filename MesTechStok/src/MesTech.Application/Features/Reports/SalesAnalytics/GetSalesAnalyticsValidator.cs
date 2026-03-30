using FluentValidation;

namespace MesTech.Application.Features.Reports.SalesAnalytics;

public sealed class GetSalesAnalyticsValidator : AbstractValidator<GetSalesAnalyticsQuery>
{
    public GetSalesAnalyticsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.To).GreaterThan(x => x.From).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
