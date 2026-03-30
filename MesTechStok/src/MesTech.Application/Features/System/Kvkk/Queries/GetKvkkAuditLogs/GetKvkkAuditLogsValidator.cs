using FluentValidation;

namespace MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;

public sealed class GetKvkkAuditLogsValidator : AbstractValidator<GetKvkkAuditLogsQuery>
{
    public GetKvkkAuditLogsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
