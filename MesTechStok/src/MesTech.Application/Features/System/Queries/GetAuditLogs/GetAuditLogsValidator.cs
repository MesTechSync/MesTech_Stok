using FluentValidation;

namespace MesTech.Application.Features.System.Queries.GetAuditLogs;

public class GetAuditLogsValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
