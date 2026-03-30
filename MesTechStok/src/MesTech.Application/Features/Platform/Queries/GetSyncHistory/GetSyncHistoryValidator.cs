using FluentValidation;

namespace MesTech.Application.Features.Platform.Queries.GetSyncHistory;

public sealed class GetSyncHistoryValidator : AbstractValidator<GetSyncHistoryQuery>
{
    public GetSyncHistoryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
        RuleFor(x => x.PlatformFilter).MaximumLength(200)
            .When(x => x.PlatformFilter is not null);
    }
}
