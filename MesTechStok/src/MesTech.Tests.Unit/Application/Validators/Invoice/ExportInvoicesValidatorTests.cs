using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands.ExportInvoices;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Invoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ExportInvoicesValidatorTests
{
    private readonly ExportInvoicesValidator _sut = new();

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
    public async Task EmptyFormat_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Format = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task InvalidFormat_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Format = "XML" };
        var result = await _sut.ValidateAsync(cmd);
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
    public async Task AllowedFormats_ShouldPass(string format)
    {
        var cmd = CreateValidCommand() with { Format = format };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NullDates_ShouldPass()
    {
        var cmd = CreateValidCommand() with { DateFrom = null, DateTo = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DateFromOnly_ShouldPass()
    {
        var cmd = CreateValidCommand() with { DateFrom = DateTime.UtcNow.AddMonths(-1), DateTo = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DateToOnly_ShouldPass()
    {
        var cmd = CreateValidCommand() with { DateFrom = null, DateTo = DateTime.UtcNow };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DateFromBeforeDateTo_ShouldPass()
    {
        var cmd = CreateValidCommand() with
        {
            DateFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTo = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DateFromAfterDateTo_ShouldFail()
    {
        var cmd = CreateValidCommand() with
        {
            DateFrom = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTo = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DateFrom");
    }

    [Fact]
    public async Task DateFromEqualToDateTo_ShouldFail()
    {
        // LessThan — esit tarihler gecersiz (from < to olmali)
        var sameDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var cmd = CreateValidCommand() with { DateFrom = sameDate, DateTo = sameDate };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DateFrom");
    }

    [Fact]
    public async Task MultipleInvalidFields_ShouldReportAll()
    {
        var cmd = CreateValidCommand() with
        {
            TenantId = Guid.Empty,
            Format = ""
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }

    private static ExportInvoicesCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Format: "XLSX",
        DateFrom: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DateTo: new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc));
}
