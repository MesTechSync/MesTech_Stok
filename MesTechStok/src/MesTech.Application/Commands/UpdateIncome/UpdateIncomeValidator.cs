using FluentValidation;

namespace MesTech.Application.Commands.UpdateIncome;

public sealed class UpdateIncomeValidator : AbstractValidator<UpdateIncomeCommand>
{
    public UpdateIncomeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note != null);
    }
}
