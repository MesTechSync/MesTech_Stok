using FluentValidation;

namespace MesTech.Application.Commands.DeleteIncome;

public sealed class DeleteIncomeValidator : AbstractValidator<DeleteIncomeCommand>
{
    public DeleteIncomeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
