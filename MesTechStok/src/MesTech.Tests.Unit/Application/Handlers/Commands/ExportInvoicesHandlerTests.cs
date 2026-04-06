using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands.ExportInvoices;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ExportInvoicesHandlerTests
{
    private ExportInvoicesHandler CreateHandler() => new();

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_XlsxFormat_ShouldReturnXlsxResult()
    {
        var handler = CreateHandler();
        var command = new ExportInvoicesCommand(Guid.NewGuid(), "xlsx");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().StartWith("faturalar_");
        result.FileName.Should().EndWith(".xlsx");
        result.ExportedCount.Should().Be(0);
        result.FileData.Length.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CsvFormat_ShouldReturnCsvResult()
    {
        var handler = CreateHandler();
        var command = new ExportInvoicesCommand(Guid.NewGuid(), "CSV");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task Handle_PdfFormat_ShouldReturnPdfResult()
    {
        var handler = CreateHandler();
        var command = new ExportInvoicesCommand(Guid.NewGuid(), "PDF");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task Handle_UnknownFormat_ShouldDefaultToXlsx()
    {
        var handler = CreateHandler();
        var command = new ExportInvoicesCommand(Guid.NewGuid(), "xml");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task Handle_WithDateRange_ShouldIncludeDatesInFileName()
    {
        var handler = CreateHandler();
        var from = new DateTime(2026, 2, 15);
        var to = new DateTime(2026, 3, 15);
        var command = new ExportInvoicesCommand(Guid.NewGuid(), DateFrom: from, DateTo: to);

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().Contain("20260215");
        result.FileName.Should().Contain("20260315");
    }

    [Fact]
    public async Task Handle_NoDates_ShouldUseAllPlaceholder()
    {
        var handler = CreateHandler();
        var command = new ExportInvoicesCommand(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().Contain("all_all");
    }
}
