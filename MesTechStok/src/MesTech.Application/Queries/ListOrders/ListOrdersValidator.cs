using FluentValidation;

namespace MesTech.Application.Queries.ListOrders;

public sealed class ListOrdersValidator : AbstractValidator<ListOrdersQuery>
{
    public ListOrdersValidator()
    {
        RuleFor(x => x.Status).MaximumLength(50).When(x => x.Status != null);
    }
}
