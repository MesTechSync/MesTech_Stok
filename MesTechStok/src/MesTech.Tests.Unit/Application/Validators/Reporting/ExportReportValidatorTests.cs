using FluentAssertions;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Reporting;

[Trait("Category", "Unit")]
public class ExportReportValidatorTests
{
    private readonly ExportReportValidator _sut = new();

    private static ExportReportCommand CreateValidCommand() =>
        new(Guid.NewGuid(), "SalesReport", "xlsx", null);

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
    public async Task ReportType_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { ReportType = "" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReportType");
    }

    [Fact]
    public async Task ReportType_WhenNull_ShouldFail()
    {
        var command = CreateValidCommand() with { ReportType = null! };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReportType");
    }

    [Fact]
    public async Task ReportType_WhenExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { ReportType = new string('r', 201) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReportType");
    }

    [Fact]
    public async Task ReportType_WhenAtMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { ReportType = new string('r', 200) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
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
        var command = CreateValidCommand() with { Format = "docx" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task Parameters_WhenNull_ShouldPass()
    {
        var command = CreateValidCommand() with { Parameters = null };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Parameters_WhenProvided_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            Parameters = new Dictionary<string, string> { { "year", "2026" }, { "quarter", "Q1" } }
        };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleFields_WhenInvalid_ShouldFailWithMultipleErrors()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty, ReportType = "", Format = "xml" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(3);
    }

}
