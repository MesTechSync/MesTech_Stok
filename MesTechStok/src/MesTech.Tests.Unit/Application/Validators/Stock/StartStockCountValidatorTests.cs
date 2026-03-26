using FluentAssertions;
using MesTech.Application.Features.Stock.Commands.StartStockCount;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class StartStockCountValidatorTests
{
    private readonly StartStockCountValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new StartStockCountCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new StartStockCountCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task DescriptionExceeds500_ShouldFail()
    {
        var cmd = new StartStockCountCommand(Guid.NewGuid(), Description: new string('D', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task NullDescription_ShouldPass()
    {
        var cmd = new StartStockCountCommand(Guid.NewGuid(), Description: null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DescriptionExactly500_ShouldPass()
    {
        var cmd = new StartStockCountCommand(Guid.NewGuid(), Description: new string('D', 500));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
