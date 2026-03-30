using FluentValidation;

namespace MesTech.Application.Queries.GetExpenseById;

public sealed class GetExpenseByIdValidator : AbstractValidator<GetExpenseByIdQuery>
{
    public GetExpenseByIdValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty).WithMessage("Geçerli gider ID gerekli.");
    }
}
