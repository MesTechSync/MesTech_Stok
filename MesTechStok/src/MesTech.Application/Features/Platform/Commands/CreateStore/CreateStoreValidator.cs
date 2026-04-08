using FluentValidation;

namespace MesTech.Application.Features.Platform.Commands.CreateStore;

public sealed class CreateStoreValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PlatformType).IsInEnum();
        RuleFor(x => x.Credentials).NotNull()
            .Must(c => c.Count > 0)
            .WithMessage("At least one credential key-value pair is required.");
        RuleForEach(x => x.Credentials)
            .Must(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
            .WithMessage("Credential key must not be empty.")
            .Must(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .WithMessage("Credential value must not be empty.");
    }
}
