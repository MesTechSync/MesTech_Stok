using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.UpdateDealStage;

public sealed class UpdateDealStageValidator : AbstractValidator<UpdateDealStageCommand>
{
    public UpdateDealStageValidator()
    {
        RuleFor(x => x.DealId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli anlaşma ID gerekli.");

        RuleFor(x => x.NewStageId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli aşama ID gerekli.");

        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId zorunlu.");
    }
}
