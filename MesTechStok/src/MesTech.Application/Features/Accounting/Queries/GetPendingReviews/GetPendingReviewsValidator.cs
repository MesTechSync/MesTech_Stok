using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetPendingReviews;

public sealed class GetPendingReviewsValidator : AbstractValidator<GetPendingReviewsQuery>
{
    public GetPendingReviewsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
    }
}
