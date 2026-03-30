using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetBitrix24Pipeline;

public sealed class GetBitrix24PipelineValidator : AbstractValidator<GetBitrix24PipelineQuery>
{
    public GetBitrix24PipelineValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StageFilter).MaximumLength(200).When(x => x.StageFilter is not null);
    }
}
