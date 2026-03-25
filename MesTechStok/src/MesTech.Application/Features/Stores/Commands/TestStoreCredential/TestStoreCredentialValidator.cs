using FluentValidation;

namespace MesTech.Application.Features.Stores.Commands.TestStoreCredential;

public sealed class TestStoreCredentialValidator : AbstractValidator<TestStoreCredentialCommand>
{
    public TestStoreCredentialValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
    }
}
