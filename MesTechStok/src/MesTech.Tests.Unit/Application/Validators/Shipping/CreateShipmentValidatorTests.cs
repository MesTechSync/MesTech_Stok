using FluentAssertions;
using MesTech.Application.Features.Shipping.Commands.CreateShipment;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Shipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateShipmentValidatorTests
{
    private readonly CreateShipmentValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyOrderId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { OrderId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public async Task CargoProviderNone_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CargoProvider = CargoProvider.None };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CargoProvider");
    }

    [Fact]
    public async Task ValidCargoProvider_YurticiKargo_ShouldPass()
    {
        var cmd = CreateValidCommand() with { CargoProvider = CargoProvider.YurticiKargo };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyRecipientName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RecipientName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientName");
    }

    [Fact]
    public async Task RecipientNameExceeds200_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RecipientName = new string('A', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientName");
    }

    [Fact]
    public async Task RecipientNameExactly200_ShouldPass()
    {
        var cmd = CreateValidCommand() with { RecipientName = new string('A', 200) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyRecipientAddress_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RecipientAddress = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientAddress");
    }

    [Fact]
    public async Task RecipientAddressExceeds500_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RecipientAddress = new string('X', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientAddress");
    }

    [Fact]
    public async Task RecipientAddressExactly500_ShouldPass()
    {
        var cmd = CreateValidCommand() with { RecipientAddress = new string('X', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyRecipientPhone_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RecipientPhone = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientPhone");
    }

    [Fact]
    public async Task RecipientPhoneExceeds20_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RecipientPhone = new string('9', 21) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientPhone");
    }

    [Fact]
    public async Task WeightZero_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Weight = 0m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Weight");
    }

    [Fact]
    public async Task WeightNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Weight = -1m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Weight");
    }

    [Fact]
    public async Task WeightExceeds150_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Weight = 150.01m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Weight");
    }

    [Fact]
    public async Task WeightExactly150_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Weight = 150m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task WeightMinimalPositive_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Weight = 0.01m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateShipmentCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        OrderId: Guid.NewGuid(),
        CargoProvider: CargoProvider.ArasKargo,
        RecipientName: "Ali Yilmaz",
        RecipientAddress: "Ataturk Cad. No:42 Kadikoy/Istanbul",
        RecipientPhone: "+905551234567",
        Weight: 2.5m,
        Notes: "Kirilacak urun");
}
