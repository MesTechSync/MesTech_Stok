using FluentValidation;

namespace MesTech.Application.Features.Platform.Commands.CreateStore;

public sealed class CreateStoreValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PlatformType).IsInEnum();
    }
}
