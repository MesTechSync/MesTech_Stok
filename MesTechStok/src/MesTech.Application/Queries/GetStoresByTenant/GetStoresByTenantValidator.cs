using FluentValidation;

namespace MesTech.Application.Queries.GetStoresByTenant;

public sealed class GetStoresByTenantValidator : AbstractValidator<GetStoresByTenantQuery>
{
    public GetStoresByTenantValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
