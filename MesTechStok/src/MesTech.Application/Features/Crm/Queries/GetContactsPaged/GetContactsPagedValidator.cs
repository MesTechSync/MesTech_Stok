using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetContactsPaged;

public sealed class GetContactsPagedValidator : AbstractValidator<GetContactsPagedQuery>
{
    public GetContactsPagedValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
    }
}
