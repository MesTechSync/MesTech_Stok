using FluentValidation;

namespace MesTech.Application.Queries.GetIncomes;

public sealed class GetIncomesValidator : AbstractValidator<GetIncomesQuery>
{
    public GetIncomesValidator() { }
}
