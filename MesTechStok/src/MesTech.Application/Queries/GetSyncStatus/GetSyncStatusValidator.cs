using FluentValidation;

namespace MesTech.Application.Queries.GetSyncStatus;

public sealed class GetSyncStatusValidator : AbstractValidator<GetSyncStatusQuery>
{
    public GetSyncStatusValidator()
    {
        RuleFor(x => x.PlatformCode).MaximumLength(50).When(x => x.PlatformCode != null);
    }
}
