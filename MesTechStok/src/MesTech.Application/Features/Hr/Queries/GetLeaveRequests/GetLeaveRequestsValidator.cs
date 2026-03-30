using FluentValidation;

namespace MesTech.Application.Features.Hr.Queries.GetLeaveRequests;

public sealed class GetLeaveRequestsValidator : AbstractValidator<GetLeaveRequestsQuery>
{
    public GetLeaveRequestsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
