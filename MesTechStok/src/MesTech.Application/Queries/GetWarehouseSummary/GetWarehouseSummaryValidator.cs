using FluentValidation;

namespace MesTech.Application.Queries.GetWarehouseSummary;

public sealed class GetWarehouseSummaryValidator : AbstractValidator<GetWarehouseSummaryQuery>
{
    public GetWarehouseSummaryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
