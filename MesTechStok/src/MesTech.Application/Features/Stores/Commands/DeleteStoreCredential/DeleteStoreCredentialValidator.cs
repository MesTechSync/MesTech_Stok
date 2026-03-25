using FluentValidation;

namespace MesTech.Application.Features.Stores.Commands.DeleteStoreCredential;

public sealed class DeleteStoreCredentialValidator : AbstractValidator<DeleteStoreCredentialCommand>
{
    public DeleteStoreCredentialValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty()
            .WithMessage("StoreId is required for credential deletion.");
    }
}
