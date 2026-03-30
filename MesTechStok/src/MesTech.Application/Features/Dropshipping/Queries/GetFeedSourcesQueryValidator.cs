using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries;

public sealed class GetFeedSourcesQueryValidator : AbstractValidator<GetFeedSourcesQuery>
{
    public GetFeedSourcesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
