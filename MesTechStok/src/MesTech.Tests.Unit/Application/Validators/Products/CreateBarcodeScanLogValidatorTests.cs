using FluentAssertions;
using MesTech.Application.Commands.CreateBarcodeScanLog;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Products;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateBarcodeScanLogValidatorTests
{
    private readonly CreateBarcodeScanLogValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Barcode_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Barcode = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    [Fact]
    public async Task Barcode_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Barcode = new string('B', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    [Fact]
    public async Task Format_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Format = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task Format_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Format = new string('F', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task Source_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Source = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Source");
    }

    [Fact]
    public async Task Source_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Source = new string('S', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Source");
    }

    [Fact]
    public async Task DeviceId_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DeviceId = new string('D', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DeviceId");
    }

    [Fact]
    public async Task DeviceId_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { DeviceId = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidationMessage_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ValidationMessage = new string('V', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ValidationMessage");
    }

    [Fact]
    public async Task ValidationMessage_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ValidationMessage = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CorrelationId_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CorrelationId = new string('C', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CorrelationId");
    }

    [Fact]
    public async Task CorrelationId_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { CorrelationId = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateBarcodeScanLogCommand CreateValidCommand() => new(
        Barcode: "8680000000001",
        Format: "EAN13",
        Source: "HandScanner",
        DeviceId: "SCANNER-01",
        IsValid: true,
        ValidationMessage: null,
        RawLength: 13,
        CorrelationId: "corr-001"
    );
}
