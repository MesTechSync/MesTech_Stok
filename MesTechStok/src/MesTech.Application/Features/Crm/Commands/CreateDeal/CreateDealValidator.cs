using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.CreateDeal;

public class CreateDealValidator : AbstractValidator<CreateDealCommand>
{
    public CreateDealValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PipelineId).NotEmpty();
        RuleFor(x => x.StageId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}
