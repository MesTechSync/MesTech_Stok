using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetPipelineKanban;

public sealed class GetPipelineKanbanValidator : AbstractValidator<GetPipelineKanbanQuery>
{
    public GetPipelineKanbanValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PipelineId)
            .NotEqual(Guid.Empty)
            .WithMessage("Pipeline kimliği boş olamaz.");
    }
}
