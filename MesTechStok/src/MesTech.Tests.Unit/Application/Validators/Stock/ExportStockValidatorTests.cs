using FluentAssertions;
using MesTech.Application.Features.Stock.Commands.ExportStock;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
public class ExportStockValidatorTests
{
    private readonly ExportStockValidator _sut = new();

    private static ExportStockCommand CreateValidCommand() =>
        new(Guid.NewGuid(), "xlsx", null);

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task Format_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { Format = "" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task Format_WhenXlsx_ShouldPass()
    {
        var command = CreateValidCommand() with { Format = "xlsx" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Format_WhenCsv_ShouldPass()
    {
        var command = CreateValidCommand() with { Format = "csv" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Format_WhenPdf_ShouldPass()
    {
        var command = CreateValidCommand() with { Format = "pdf" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Format_WhenInvalid_ShouldFail()
    {
        var command = CreateValidCommand() with { Format = "json" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task Filter_WhenNull_ShouldPass()
    {
        var command = CreateValidCommand() with { Filter = null };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Filter_WhenWithinMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { Filter = "category:electronics" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Filter_WhenExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { Filter = new string('f', 501) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Filter");
    }

    [Fact]
    public async Task Filter_WhenAtMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { Filter = new string('f', 500) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

}
