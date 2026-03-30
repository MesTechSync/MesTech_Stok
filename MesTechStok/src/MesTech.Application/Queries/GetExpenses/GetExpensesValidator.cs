using FluentValidation;

namespace MesTech.Application.Queries.GetExpenses;

public sealed class GetExpensesValidator : AbstractValidator<GetExpensesQuery>
{
    public GetExpensesValidator() { }
}
