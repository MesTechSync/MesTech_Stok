using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;

public sealed class GetCashFlowReportValidator : AbstractValidator<GetCashFlowReportQuery>
{
    public GetCashFlowReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.From).NotEmpty();
        RuleFor(x => x.To).NotEmpty().GreaterThanOrEqualTo(x => x.From);
    }
}
