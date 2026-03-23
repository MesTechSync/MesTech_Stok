using FluentValidation;

namespace MesTech.Application.Features.Logging.Queries.GetLogs;

public class GetLogsValidator : AbstractValidator<GetLogsQuery>
{
    public GetLogsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 500);
    }
}
