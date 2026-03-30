using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.ListFixedAssets;

public sealed class ListFixedAssetsValidator : AbstractValidator<ListFixedAssetsQuery>
{
    public ListFixedAssetsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
