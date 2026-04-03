using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Features.Cargo.Queries.GetShipmentLabel;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Features.Cargo;

[Trait("Category", "Unit")]
public class GetShipmentLabelHandlerTests
{
    private readonly Mock<ILogger<GetShipmentLabelHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_NoAdaptersWithLabelSupport_ReturnsError()
    {
        var adapters = new List<ICargoAdapter>();
        var sut = new GetShipmentLabelHandler(adapters, _loggerMock.Object);

        var result = await sut.Handle(
            new GetShipmentLabelQuery(Guid.NewGuid(), "SHP-001"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Etiket");
    }

    [Fact]
    public async Task Handle_AdapterReturnsLabel_ReturnsSuccess()
    {
        var adapterMock = new Mock<ICargoAdapter>();
        adapterMock.Setup(a => a.SupportsLabelGeneration).Returns(true);
        adapterMock.Setup(a => a.GetShipmentLabelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LabelResult
            {
                Data = new byte[] { 0x25, 0x50, 0x44, 0x46 },
                FileName = "label.pdf",
                Format = LabelFormat.Pdf
            });

        var sut = new GetShipmentLabelHandler(new[] { adapterMock.Object }, _loggerMock.Object);

        var result = await sut.Handle(
            new GetShipmentLabelQuery(Guid.NewGuid(), "SHP-001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ContentType.Should().Be("application/pdf");
    }
}
