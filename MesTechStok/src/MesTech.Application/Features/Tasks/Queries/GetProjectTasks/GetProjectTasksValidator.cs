using FluentValidation;

namespace MesTech.Application.Features.Tasks.Queries.GetProjectTasks;

public sealed class GetProjectTasksValidator : AbstractValidator<GetProjectTasksQuery>
{
    public GetProjectTasksValidator()
    {
        RuleFor(x => x.ProjectId).NotEqual(Guid.Empty)
            .WithMessage("Proje kimlik bilgisi bos olamaz.");
    }
}
