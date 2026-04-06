using FluentAssertions;
using MesTech.Application.Features.Shipping.Queries.DownloadShipmentLabel;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class DownloadShipmentLabelHandlerTests
{
    private DownloadShipmentLabelHandler CreateHandler() => new();

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_PdfFormat_ShouldReturnPdfResult()
    {
        var handler = CreateHandler();
        var query = new DownloadShipmentLabelQuery(Guid.NewGuid(), Guid.NewGuid(), Format: "PDF");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
        result.FileName.Should().StartWith("etiket_");
    }

    [Fact]
    public async Task Handle_ZplFormat_ShouldReturnZplResult()
    {
        var handler = CreateHandler();
        var query = new DownloadShipmentLabelQuery(Guid.NewGuid(), Guid.NewGuid(), Format: "ZPL");

        var result = await handler.Handle(query, CancellationToken.None);

        result.ContentType.Should().Be("application/x-zpl");
        result.FileName.Should().EndWith(".zpl");
    }

    [Fact]
    public async Task Handle_PngFormat_ShouldReturnPngResult()
    {
        var handler = CreateHandler();
        var query = new DownloadShipmentLabelQuery(Guid.NewGuid(), Guid.NewGuid(), Format: "PNG");

        var result = await handler.Handle(query, CancellationToken.None);

        result.ContentType.Should().Be("image/png");
        result.FileName.Should().EndWith(".png");
    }

    [Fact]
    public async Task Handle_WithTrackingNumber_ShouldIncludeInFileName()
    {
        var handler = CreateHandler();
        var query = new DownloadShipmentLabelQuery(
            Guid.NewGuid(), Guid.NewGuid(), TrackingNumber: "TRK123456");

        var result = await handler.Handle(query, CancellationToken.None);

        result.FileName.Should().Contain("TRK123456");
    }

    [Fact]
    public async Task Handle_WithoutTrackingNumber_ShouldUseShipmentIdPrefix()
    {
        var shipmentId = Guid.NewGuid();
        var handler = CreateHandler();
        var query = new DownloadShipmentLabelQuery(Guid.NewGuid(), shipmentId);

        var result = await handler.Handle(query, CancellationToken.None);

        var expectedPrefix = shipmentId.ToString("N")[..8];
        result.FileName.Should().Contain(expectedPrefix);
    }

    [Fact]
    public async Task Handle_LabelDataShouldBeEmpty_StubImplementation()
    {
        var handler = CreateHandler();
        var query = new DownloadShipmentLabelQuery(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.LabelData.Length.Should().Be(0);
    }
}
