using FluentValidation;

namespace MesTech.Application.Commands.ProcessAiRecommendation;

public sealed class ProcessAiRecommendationValidator : AbstractValidator<ProcessAiRecommendationCommand>
{
    public ProcessAiRecommendationValidator()
    {
        RuleFor(x => x.RecommendationType).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Priority).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
