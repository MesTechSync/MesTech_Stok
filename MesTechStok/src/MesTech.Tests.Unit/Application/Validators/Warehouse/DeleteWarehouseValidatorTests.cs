using FluentAssertions;
using MesTech.Application.Commands.DeleteWarehouse;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Warehouse;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DeleteWarehouseValidatorTests
{
    private readonly DeleteWarehouseValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new DeleteWarehouseCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = new DeleteWarehouseCommand(Guid.Empty, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task WarehouseId_WhenEmpty_ShouldFail()
    {
        var cmd = new DeleteWarehouseCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WarehouseId");
    }

    [Fact]
    public async Task BothIds_WhenEmpty_ShouldFail()
    {
        var cmd = new DeleteWarehouseCommand(Guid.Empty, Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
