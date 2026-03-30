using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetFixedAssets;

public sealed class GetFixedAssetsValidator : AbstractValidator<GetFixedAssetsQuery>
{
    public GetFixedAssetsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
