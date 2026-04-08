using FluentValidation;

namespace MesTech.Application.Features.Platform.Commands.UpdateStore;

public sealed class UpdateStoreValidator : AbstractValidator<UpdateStoreCommand>
{
    public UpdateStoreValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(500);
    }
}
