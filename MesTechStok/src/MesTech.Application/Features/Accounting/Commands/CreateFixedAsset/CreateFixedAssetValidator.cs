using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;

public class CreateFixedAssetValidator : AbstractValidator<CreateFixedAssetCommand>
{
    public CreateFixedAssetValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AssetCode).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AcquisitionCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}
