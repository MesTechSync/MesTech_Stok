using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ParseAndImportSettlement;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

[Trait("Category", "Unit")]
public class ParseAndImportSettlementValidatorTests
{
    private readonly ParseAndImportSettlementValidator _sut = new();

    private static ParseAndImportSettlementCommand CreateValidCommand() =>
        new(
            TenantId: Guid.NewGuid(),
            Platform: "Trendyol",
            RawData: new byte[] { 0x01, 0x02, 0x03 },
            Format: "JSON");

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
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
    public async Task EmptyPlatform_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Platform = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public async Task PlatformExceeds50Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Platform = new string('X', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public async Task PlatformExactly50Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Platform = new string('X', 50) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyRawData_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RawData = Array.Empty<byte>() };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RawData");
    }

    [Fact]
    public async Task EmptyFormat_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Format = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task UnsupportedFormat_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Format = "YAML" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Theory]
    [InlineData("JSON")]
    [InlineData("json")]
    [InlineData("TSV")]
    [InlineData("tsv")]
    [InlineData("CSV")]
    [InlineData("csv")]
    [InlineData("XML")]
    [InlineData("xml")]
    public async Task SupportedFormats_ShouldPass(string format)
    {
        var cmd = CreateValidCommand() with { Format = format };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AllFieldsInvalid_ShouldHaveMultipleErrors()
    {
        var cmd = new ParseAndImportSettlementCommand(
            TenantId: Guid.Empty,
            Platform: "",
            RawData: Array.Empty<byte>(),
            Format: "INVALID");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(4);
    }

    [Fact]
    public async Task MixedCaseFormat_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Format = "Json" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
