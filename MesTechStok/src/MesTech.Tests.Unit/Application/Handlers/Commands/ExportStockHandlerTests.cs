using FluentAssertions;
using MesTech.Application.Features.Stock.Commands.ExportStock;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ExportStockHandlerTests
{
    private ExportStockHandler CreateHandler() => new();

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
        var command = new ExportStockCommand(Guid.NewGuid(), "xlsx");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().EndWith(".xlsx");
        result.ExportedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CsvFormat_ShouldReturnCsvResult()
    {
        var handler = CreateHandler();
        var command = new ExportStockCommand(Guid.NewGuid(), "CSV");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task Handle_PdfFormat_ShouldReturnPdfResult()
    {
        var handler = CreateHandler();
        var command = new ExportStockCommand(Guid.NewGuid(), "PDF");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task Handle_DefaultFormat_ShouldReturnXlsx()
    {
        var handler = CreateHandler();
        var command = new ExportStockCommand(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().Contain("stok_");
        result.FileName.Should().EndWith(".xlsx");
    }
}
