using FluentAssertions;
using MesTech.Application.Features.Shipping.Commands.BatchShipOrders;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Shipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class BatchShipOrdersValidatorTests
{
    private readonly BatchShipOrdersValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new BatchShipOrdersCommand(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() });
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new BatchShipOrdersCommand(Guid.Empty, new List<Guid> { Guid.NewGuid() });
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}
