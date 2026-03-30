using FluentValidation;

namespace MesTech.Application.Features.Returns.Queries.GetReturnList;

public sealed class GetReturnListValidator : AbstractValidator<GetReturnListQuery>
{
    public GetReturnListValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
    }
}
