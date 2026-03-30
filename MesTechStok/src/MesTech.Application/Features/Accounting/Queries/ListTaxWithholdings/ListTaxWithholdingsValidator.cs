using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.ListTaxWithholdings;

public sealed class ListTaxWithholdingsValidator : AbstractValidator<ListTaxWithholdingsQuery>
{
    public ListTaxWithholdingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
