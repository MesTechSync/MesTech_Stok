using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;

public sealed class GetDropshipSuppliersValidator : AbstractValidator<GetDropshipSuppliersQuery>
{
    public GetDropshipSuppliersValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
