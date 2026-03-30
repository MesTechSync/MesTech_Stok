using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetOrdersPending;

public sealed class GetOrdersPendingValidator : AbstractValidator<GetOrdersPendingQuery>
{
    public GetOrdersPendingValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
