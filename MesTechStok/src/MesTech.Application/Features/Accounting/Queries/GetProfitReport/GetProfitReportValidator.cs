using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetProfitReport;

public sealed class GetProfitReportValidator : AbstractValidator<GetProfitReportQuery>
{
    public GetProfitReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Period).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Platform).MaximumLength(50)
            .When(x => x.Platform is not null);
    }
}
