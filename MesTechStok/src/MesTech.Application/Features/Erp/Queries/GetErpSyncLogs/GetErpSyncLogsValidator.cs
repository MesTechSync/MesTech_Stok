using FluentValidation;

namespace MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;

public sealed class GetErpSyncLogsValidator : AbstractValidator<GetErpSyncLogsQuery>
{
    public GetErpSyncLogsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
