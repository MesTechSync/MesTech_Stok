using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries;

public sealed class GetFeedImportHistoryQueryValidator : AbstractValidator<GetFeedImportHistoryQuery>
{
    public GetFeedImportHistoryQueryValidator()
    {
        RuleFor(x => x.FeedId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
