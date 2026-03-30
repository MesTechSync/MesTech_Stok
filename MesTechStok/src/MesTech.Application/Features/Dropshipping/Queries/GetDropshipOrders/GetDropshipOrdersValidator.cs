using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipOrders;

public sealed class GetDropshipOrdersValidator : AbstractValidator<GetDropshipOrdersQuery>
{
    public GetDropshipOrdersValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
