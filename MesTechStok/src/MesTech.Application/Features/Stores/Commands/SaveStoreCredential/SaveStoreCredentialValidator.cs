using FluentValidation;

namespace MesTech.Application.Features.Stores.Commands.SaveStoreCredential;

public class SaveStoreCredentialValidator : AbstractValidator<SaveStoreCredentialCommand>
{
    private static readonly string[] ValidCredentialTypes = { "api_key", "oauth2", "soap" };

    public SaveStoreCredentialValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty()
            .WithMessage("StoreId is required.");

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.Platform)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Platform is required.");

        RuleFor(x => x.CredentialType)
            .NotEmpty()
            .Must(t => ValidCredentialTypes.Contains(t, StringComparer.Ordinal))
            .WithMessage("CredentialType must be one of: api_key, oauth2, soap.");

        RuleFor(x => x.Fields)
            .NotEmpty()
            .WithMessage("At least one credential field is required.");

        RuleForEach(x => x.Fields)
            .Must(kv => !string.IsNullOrWhiteSpace(kv.Key))
            .WithMessage("Credential field key cannot be empty.");

        RuleForEach(x => x.Fields)
            .Must(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .WithMessage("Credential field value cannot be empty.");
    }
}
