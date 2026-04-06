using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands.ExportInvoiceReport;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ExportInvoiceReportHandlerTests
{
    private ExportInvoiceReportHandler CreateHandler() => new();

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_XlsxFormat_ShouldReturnXlsxFileName()
    {
        var handler = CreateHandler();
        var command = new ExportInvoiceReportCommand(Guid.NewGuid(), "xlsx");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().EndWith(".xlsx");
        result.FileName.Should().StartWith("fatura_rapor_");
        result.ExportedCount.Should().Be(0);
        result.FileData.Length.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CsvFormat_ShouldReturnCsvFileName()
    {
        var handler = CreateHandler();
        var command = new ExportInvoiceReportCommand(Guid.NewGuid(), "CSV");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task Handle_PdfFormat_ShouldReturnPdfFileName()
    {
        var handler = CreateHandler();
        var command = new ExportInvoiceReportCommand(Guid.NewGuid(), "PDF");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task Handle_UnknownFormat_ShouldDefaultToXlsx()
    {
        var handler = CreateHandler();
        var command = new ExportInvoiceReportCommand(Guid.NewGuid(), "unknown");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task Handle_WithPeriod_ShouldIncludePeriodInFileName()
    {
        var handler = CreateHandler();
        var command = new ExportInvoiceReportCommand(Guid.NewGuid(), Period: "monthly");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().Contain("monthly");
    }

    [Fact]
    public async Task Handle_WithDateRange_ShouldIncludeDatesInFileName()
    {
        var handler = CreateHandler();
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 3, 31);
        var command = new ExportInvoiceReportCommand(Guid.NewGuid(), DateFrom: from, DateTo: to);

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().Contain("20260101");
        result.FileName.Should().Contain("20260331");
    }

    [Fact]
    public async Task Handle_NoDates_ShouldUseAllPlaceholder()
    {
        var handler = CreateHandler();
        var command = new ExportInvoiceReportCommand(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().Contain("all_all");
    }
}
