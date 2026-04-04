using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.ExportCustomers;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ExportCustomersHandlerTests
{
    private ExportCustomersHandler CreateHandler() => new();

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
        var command = new ExportCustomersCommand(Guid.NewGuid(), "xlsx");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().EndWith(".xlsx");
        result.FileName.Should().StartWith("musteriler_");
        result.ExportedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CsvFormat_ShouldReturnCsvResult()
    {
        var handler = CreateHandler();
        var command = new ExportCustomersCommand(Guid.NewGuid(), "CSV");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task Handle_PdfFormat_ShouldReturnPdfResult()
    {
        var handler = CreateHandler();
        var command = new ExportCustomersCommand(Guid.NewGuid(), "PDF");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task Handle_UnknownFormat_ShouldDefaultToXlsx()
    {
        var handler = CreateHandler();
        var command = new ExportCustomersCommand(Guid.NewGuid(), "UNKNOWN");

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileName.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task Handle_FileDataShouldBeEmpty()
    {
        var handler = CreateHandler();
        var command = new ExportCustomersCommand(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.FileData.Length.Should().Be(0);
    }
}
