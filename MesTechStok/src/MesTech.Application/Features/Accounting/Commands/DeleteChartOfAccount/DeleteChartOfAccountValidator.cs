using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;

public sealed class DeleteChartOfAccountValidator : AbstractValidator<DeleteChartOfAccountCommand>
{
    public DeleteChartOfAccountValidator()
    {
        RuleFor(x => x.Id).NotEmpty()
            .WithMessage("Account ID is required for deletion.");
    }
}
