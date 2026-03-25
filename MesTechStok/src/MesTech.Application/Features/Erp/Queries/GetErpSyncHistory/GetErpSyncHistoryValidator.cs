using FluentValidation;

namespace MesTech.Application.Features.Erp.Queries.GetErpSyncHistory;

public sealed class GetErpSyncHistoryValidator : AbstractValidator<GetErpSyncHistoryQuery>
{
    public GetErpSyncHistoryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
