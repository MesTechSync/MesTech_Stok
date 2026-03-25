using FluentValidation;

namespace MesTech.Application.Features.Tasks.Commands.CreateWorkTask;

public sealed class CreateWorkTaskValidator : AbstractValidator<CreateWorkTaskCommand>
{
    public CreateWorkTaskValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}
