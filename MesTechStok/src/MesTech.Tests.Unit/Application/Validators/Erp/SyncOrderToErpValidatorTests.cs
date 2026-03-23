using FluentAssertions;
using MesTech.Application.Features.Erp.Commands.SyncOrderToErp;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Erp;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SyncOrderToErpValidatorTests
{
    private readonly SyncOrderToErpValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new SyncOrderToErpCommand(Guid.NewGuid(), Guid.NewGuid(), ErpProvider.Parasut);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new SyncOrderToErpCommand(Guid.Empty, Guid.NewGuid(), ErpProvider.Parasut);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyOrderId_ShouldFail()
    {
        var cmd = new SyncOrderToErpCommand(Guid.NewGuid(), Guid.Empty, ErpProvider.Parasut);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }
}
