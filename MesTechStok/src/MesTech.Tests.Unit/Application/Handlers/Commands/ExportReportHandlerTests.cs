using FluentAssertions;
using MesTech.Application.Features.Reporting.Commands.ExportReport;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ExportReportHandlerTests
{
    private ExportReportHandler CreateHandler() => new();

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
        var command = new ExportReportCommand(Guid.NewGuid(), "Sales Report", "xlsx");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().StartWith("rapor_");
        result.FileName.Should().EndWith(".xlsx");
        result.ExportedCount.Should().Be(0);
        result.FileData.Length.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CsvFormat_ShouldReturnCsvResult()
    {
        var handler = CreateHandler();
        var command = new ExportReportCommand(Guid.NewGuid(), "Stock", "CSV");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task Handle_PdfFormat_ShouldReturnPdfResult()
    {
        var handler = CreateHandler();
        var command = new ExportReportCommand(Guid.NewGuid(), "Monthly", "PDF");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task Handle_UnknownFormat_ShouldDefaultToXlsx()
    {
        var handler = CreateHandler();
        var command = new ExportReportCommand(Guid.NewGuid(), "Test", "html");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task Handle_ReportTypeWithSpaces_ShouldSlugify()
    {
        var handler = CreateHandler();
        var command = new ExportReportCommand(Guid.NewGuid(), "Sales Report");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().Contain("SALES_REPORT");
    }

    [Fact]
    public async Task Handle_ReportTypeWithSlashes_ShouldReplaceWithUnderscore()
    {
        var handler = CreateHandler();
        var command = new ExportReportCommand(Guid.NewGuid(), "Q1/Q2 Summary");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().Contain("Q1_Q2_SUMMARY");
    }

    [Fact]
    public async Task Handle_WithParameters_ShouldStillReturnResult()
    {
        var handler = CreateHandler();
        var parameters = new Dictionary<string, string> { ["warehouse"] = "main" };
        var command = new ExportReportCommand(Guid.NewGuid(), "Inventory", Parameters: parameters);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().Contain("INVENTORY");
    }
}
