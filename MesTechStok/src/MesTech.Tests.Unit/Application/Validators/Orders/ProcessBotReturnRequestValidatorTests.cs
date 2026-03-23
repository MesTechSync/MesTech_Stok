using FluentAssertions;
using MesTech.Application.Commands.ProcessBotReturnRequest;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Orders;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ProcessBotReturnRequestValidatorTests
{
    private readonly ProcessBotReturnRequestValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CustomerPhone_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerPhone = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerPhone");
    }

    [Fact]
    public async Task CustomerPhone_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerPhone = new string('5', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerPhone");
    }

    [Fact]
    public async Task OrderNumber_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { OrderNumber = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderNumber");
    }

    [Fact]
    public async Task OrderNumber_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { OrderNumber = new string('O', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderNumber");
    }

    [Fact]
    public async Task RequestChannel_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RequestChannel = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RequestChannel");
    }

    [Fact]
    public async Task RequestChannel_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RequestChannel = new string('R', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RequestChannel");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static ProcessBotReturnRequestCommand CreateValidCommand() => new()
    {
        CustomerPhone = "+905551234567",
        OrderNumber = "ORD-2026-002",
        ReturnReason = "Defective product",
        RequestChannel = "WhatsApp",
        TenantId = Guid.NewGuid()
    };
}
