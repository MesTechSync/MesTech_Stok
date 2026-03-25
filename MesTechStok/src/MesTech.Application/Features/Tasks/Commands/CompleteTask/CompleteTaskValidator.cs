using FluentValidation;

namespace MesTech.Application.Features.Tasks.Commands.CompleteTask;

public sealed class CompleteTaskValidator : AbstractValidator<CompleteTaskCommand>
{
    public CompleteTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
