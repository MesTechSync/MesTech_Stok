using FluentAssertions;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Shipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class AutoShipOrderValidatorTests
{
    private readonly AutoShipOrderValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new AutoShipOrderCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new AutoShipOrderCommand(Guid.Empty, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyOrderId_ShouldFail()
    {
        var cmd = new AutoShipOrderCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }
}
