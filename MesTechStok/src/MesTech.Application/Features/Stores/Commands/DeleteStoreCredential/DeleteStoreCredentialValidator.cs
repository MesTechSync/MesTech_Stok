using FluentValidation;

namespace MesTech.Application.Features.Stores.Commands.DeleteStoreCredential;

public class DeleteStoreCredentialValidator : AbstractValidator<DeleteStoreCredentialCommand>
{
    public DeleteStoreCredentialValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty()
            .WithMessage("StoreId is required for credential deletion.");
    }
}
