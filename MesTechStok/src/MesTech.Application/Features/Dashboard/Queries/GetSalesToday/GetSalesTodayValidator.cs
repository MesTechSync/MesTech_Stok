using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetSalesToday;

public sealed class GetSalesTodayValidator : AbstractValidator<GetSalesTodayQuery>
{
    public GetSalesTodayValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
