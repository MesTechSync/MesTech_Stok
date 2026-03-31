using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands.ExportInvoiceReport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Invoice;

public class ExportInvoiceReportValidatorTests
{
    private readonly ExportInvoiceReportValidator _sut = new();

    private static ExportInvoiceReportCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Format: "xlsx",
        DateFrom: null,
        DateTo: null,
        Period: null);

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EmptyTenantId_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyFormat_ShouldFail()
    {
        var command = CreateValidCommand() with { Format = "" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Theory]
    [InlineData("xlsx")]
    [InlineData("csv")]
    [InlineData("pdf")]
    [InlineData("XLSX")]
    [InlineData("CSV")]
    [InlineData("PDF")]
    [Trait("Category", "Unit")]
    public async Task AllowedFormats_ShouldPass(string format)
    {
        var command = CreateValidCommand() with { Format = format };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DisallowedFormat_ShouldFail()
    {
        var command = CreateValidCommand() with { Format = "xml" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PeriodExceeds100Chars_ShouldFail()
    {
        var command = CreateValidCommand() with { Period = new string('A', 101) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Period");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Period100Chars_ShouldPass()
    {
        var command = CreateValidCommand() with { Period = new string('A', 100) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DateFromAfterDateTo_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            DateFrom = new DateTime(2026, 6, 1),
            DateTo = new DateTime(2026, 1, 1)
        };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DateFromBeforeDateTo_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            DateFrom = new DateTime(2026, 1, 1),
            DateTo = new DateTime(2026, 6, 1)
        };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task OnlyDateFromSet_ShouldPass()
    {
        var command = CreateValidCommand() with { DateFrom = new DateTime(2026, 1, 1), DateTo = null };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task NullPeriod_ShouldPass()
    {
        var command = CreateValidCommand() with { Period = null };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }
}
