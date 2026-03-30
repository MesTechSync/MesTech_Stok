using FluentValidation;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;

public sealed class GetPlatformSyncStatusValidator : AbstractValidator<GetPlatformSyncStatusQuery>
{
    public GetPlatformSyncStatusValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
