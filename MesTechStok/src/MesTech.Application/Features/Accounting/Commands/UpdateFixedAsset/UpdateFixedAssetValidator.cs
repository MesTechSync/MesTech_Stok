using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.UpdateFixedAsset;

public class UpdateFixedAssetValidator : AbstractValidator<UpdateFixedAssetCommand>
{
    public UpdateFixedAssetValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}
