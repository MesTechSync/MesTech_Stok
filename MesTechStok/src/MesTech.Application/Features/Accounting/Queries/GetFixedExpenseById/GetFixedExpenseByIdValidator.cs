using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetFixedExpenseById;

public sealed class GetFixedExpenseByIdValidator : AbstractValidator<GetFixedExpenseByIdQuery>
{
    public GetFixedExpenseByIdValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
