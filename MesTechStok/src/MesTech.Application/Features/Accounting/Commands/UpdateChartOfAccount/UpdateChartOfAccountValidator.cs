using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;

public sealed class UpdateChartOfAccountValidator : AbstractValidator<UpdateChartOfAccountCommand>
{
    public UpdateChartOfAccountValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
