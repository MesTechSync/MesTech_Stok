using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries;

public sealed class GetExportHistoryQueryValidator : AbstractValidator<GetExportHistoryQuery>
{
    public GetExportHistoryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
