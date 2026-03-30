using FluentValidation;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformList;

public sealed class GetPlatformListValidator : AbstractValidator<GetPlatformListQuery>
{
    public GetPlatformListValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
