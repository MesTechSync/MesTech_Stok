using FluentAssertions;
using MesTech.Application.Commands.UpdateWarehouse;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Warehouse;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateWarehouseValidatorTests
{
    private readonly UpdateWarehouseValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task WarehouseId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { WarehouseId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WarehouseId");
    }

    [Fact]
    public async Task Name_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExceeds200Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = new string('N', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Code_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Code = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenExceeds50Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Code = new string('C', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenExactly50Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Code = new string('C', 50) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static UpdateWarehouseCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        WarehouseId: Guid.NewGuid(),
        Name: "Ana Depo",
        Code: "WH-001",
        Description: "Ana depo aciklamasi",
        Type: "Standard",
        IsActive: true
    );
}
