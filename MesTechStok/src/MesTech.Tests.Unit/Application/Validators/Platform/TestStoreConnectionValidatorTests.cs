using FluentAssertions;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Platform;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class TestStoreConnectionValidatorTests
{
    private readonly TestStoreConnectionValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new TestStoreConnectionCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyStoreId_ShouldFail()
    {
        var cmd = new TestStoreConnectionCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreId");
    }
}
