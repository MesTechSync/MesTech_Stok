using FluentAssertions;
using MesTech.Application.Features.Stores.Commands.TestStoreCredential;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stores;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class TestStoreCredentialValidatorTests
{
    private readonly TestStoreCredentialValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new TestStoreCredentialCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyStoreId_ShouldFail()
    {
        var cmd = new TestStoreCredentialCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreId");
    }
}
