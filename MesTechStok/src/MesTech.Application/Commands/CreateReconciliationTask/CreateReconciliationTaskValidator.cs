using FluentValidation;

namespace MesTech.Application.Commands.CreateReconciliationTask;

public class CreateReconciliationTaskValidator : AbstractValidator<CreateReconciliationTaskCommand>
{
    public CreateReconciliationTaskValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
