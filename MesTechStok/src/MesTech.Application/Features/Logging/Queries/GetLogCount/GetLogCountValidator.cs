using FluentValidation;

namespace MesTech.Application.Features.Logging.Queries.GetLogCount;

public class GetLogCountValidator : AbstractValidator<GetLogCountQuery>
{
    public GetLogCountValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
