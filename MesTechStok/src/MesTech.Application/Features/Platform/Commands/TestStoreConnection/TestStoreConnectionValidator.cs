using FluentValidation;

namespace MesTech.Application.Features.Platform.Commands.TestStoreConnection;

public class TestStoreConnectionValidator : AbstractValidator<TestStoreConnectionCommand>
{
    public TestStoreConnectionValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
    }
}
