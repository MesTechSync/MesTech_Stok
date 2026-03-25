using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.DeactivateFixedAsset;

public sealed class DeactivateFixedAssetValidator : AbstractValidator<DeactivateFixedAssetCommand>
{
    public DeactivateFixedAssetValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
