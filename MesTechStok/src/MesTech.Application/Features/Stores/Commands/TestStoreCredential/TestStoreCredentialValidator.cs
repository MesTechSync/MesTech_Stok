using FluentValidation;

namespace MesTech.Application.Features.Stores.Commands.TestStoreCredential;

public class TestStoreCredentialValidator : AbstractValidator<TestStoreCredentialCommand>
{
    public TestStoreCredentialValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
    }
}
