using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.ExportCustomers;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
public class ExportCustomersValidatorTests
{
    private readonly ExportCustomersValidator _sut = new();

    private static ExportCustomersCommand CreateValidCommand() =>
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
        var command = CreateValidCommand() with { Format = "xml" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task SearchTerm_WhenNull_ShouldPass()
    {
        var command = CreateValidCommand() with { SearchTerm = null };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SearchTerm_WhenWithinMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { SearchTerm = "test search" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SearchTerm_WhenExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { SearchTerm = new string('s', 501) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SearchTerm");
    }

    [Fact]
    public async Task SearchTerm_WhenAtMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { SearchTerm = new string('s', 500) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

}
