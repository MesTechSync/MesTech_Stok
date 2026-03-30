using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries;

public sealed class GetFeedSourceByIdQueryValidator : AbstractValidator<GetFeedSourceByIdQuery>
{
    public GetFeedSourceByIdQueryValidator()
    {
        RuleFor(x => x.FeedId).NotEmpty();
    }
}
