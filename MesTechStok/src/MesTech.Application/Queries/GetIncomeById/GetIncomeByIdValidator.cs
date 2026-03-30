using FluentValidation;

namespace MesTech.Application.Queries.GetIncomeById;

public sealed class GetIncomeByIdValidator : AbstractValidator<GetIncomeByIdQuery>
{
    public GetIncomeByIdValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty).WithMessage("Geçerli gelir ID gerekli.");
    }
}
