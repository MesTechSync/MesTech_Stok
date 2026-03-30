using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipProducts;

public sealed class GetDropshipProductsValidator : AbstractValidator<GetDropshipProductsQuery>
{
    public GetDropshipProductsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
