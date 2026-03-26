using FluentAssertions;
using MesTech.Application.Commands.SyncN11Products;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Platform;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SyncN11ProductsValidatorTests
{
    private readonly SyncN11ProductsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new SyncN11ProductsCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyStoreId_ShouldFail()
    {
        var cmd = new SyncN11ProductsCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreId");
    }
}
