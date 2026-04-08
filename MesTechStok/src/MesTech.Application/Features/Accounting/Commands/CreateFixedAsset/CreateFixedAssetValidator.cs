using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;

public sealed class CreateFixedAssetValidator : AbstractValidator<CreateFixedAssetCommand>
{
    public CreateFixedAssetValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AssetCode).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AcquisitionCost).GreaterThan(0).WithMessage("Edinim maliyeti sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}
