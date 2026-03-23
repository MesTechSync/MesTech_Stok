using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.PlaceDropshipOrder;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class PlaceDropshipOrderValidatorTests
{
    private readonly PlaceDropshipOrderValidator _sut = new();

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
    public async Task OrderId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { OrderId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public async Task SupplierId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SupplierId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierId");
    }

    [Fact]
    public async Task ProductId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public async Task SupplierOrderRef_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SupplierOrderRef = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierOrderRef");
    }

    [Fact]
    public async Task SupplierOrderRef_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SupplierOrderRef = new string('R', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierOrderRef");
    }

    private static PlaceDropshipOrderCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        OrderId: Guid.NewGuid(),
        SupplierId: Guid.NewGuid(),
        ProductId: Guid.NewGuid(),
        SupplierOrderRef: "SUP-ORD-12345"
    );
}
