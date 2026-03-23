using FluentAssertions;
using MesTech.Application.Commands.SyncCiceksepetiProducts;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Platform;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SyncCiceksepetiProductsValidatorTests
{
    private readonly SyncCiceksepetiProductsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task StoreId_WhenEmpty_ShouldFail()
    {
        var cmd = new SyncCiceksepetiProductsCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreId");
    }

    private static SyncCiceksepetiProductsCommand CreateValidCommand() => new(Guid.NewGuid());
}
