using FluentValidation;

namespace MesTech.Application.Features.Tasks.Queries.GetProjects;

public sealed class GetProjectsValidator : AbstractValidator<GetProjectsQuery>
{
    public GetProjectsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
