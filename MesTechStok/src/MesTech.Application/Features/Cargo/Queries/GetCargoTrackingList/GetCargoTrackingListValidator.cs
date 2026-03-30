using FluentValidation;

namespace MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;

public sealed class GetCargoTrackingListValidator : AbstractValidator<GetCargoTrackingListQuery>
{
    public GetCargoTrackingListValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
    }
}
