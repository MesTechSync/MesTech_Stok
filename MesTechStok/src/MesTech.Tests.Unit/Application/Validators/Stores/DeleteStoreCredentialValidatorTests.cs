using FluentAssertions;
using MesTech.Application.Features.Stores.Commands.DeleteStoreCredential;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stores;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DeleteStoreCredentialValidatorTests
{
    private readonly DeleteStoreCredentialValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new DeleteStoreCredentialCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyStoreId_ShouldFail()
    {
        var cmd = new DeleteStoreCredentialCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreId");
    }
}
