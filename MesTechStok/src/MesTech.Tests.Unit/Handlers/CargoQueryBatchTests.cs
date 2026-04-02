using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;
using MesTech.Application.Features.Cargo.Queries.GetShipmentLabel;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ════════════════════════════════════════════════════════
// DEV5 TUR 6: Cargo handler batch tests — 2 handler
// Pattern: single-repo query handler → mock repo, verify call
// ════════════════════════════════════════════════════════

#region GetCargoTrackingList

[Trait("Category", "Unit")]
[Trait("Layer", "Cargo")]
public class GetCargoTrackingListHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallOrderRepo()
    {
        var repo = new Mock<IOrderRepository>();
        var logger = new Mock<ILogger<GetCargoTrackingListHandler>>();
        repo.Setup(r => r.GetRecentAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
        var sut = new GetCargoTrackingListHandler(repo.Object, logger.Object);
        await sut.Handle(new GetCargoTrackingListQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetRecentAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetShipmentLabel

[Trait("Category", "Unit")]
[Trait("Layer", "Cargo")]
public class GetShipmentLabelHandlerTests
{
    [Fact]
    public async Task Handle_WithNoAdapters_ShouldReturnError()
    {
        var adapters = Enumerable.Empty<ICargoAdapter>();
        var logger = new Mock<ILogger<GetShipmentLabelHandler>>();
        var sut = new GetShipmentLabelHandler(adapters, logger.Object);
        var result = await sut.Handle(new GetShipmentLabelQuery(Guid.NewGuid(), "SHP-001"), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithSupportingAdapter_ShouldCallGetShipmentLabelAsync()
    {
        var adapter = new Mock<ICargoAdapter>();
        adapter.Setup(a => a.SupportsLabelGeneration).Returns(true);
        adapter.Setup(a => a.GetShipmentLabelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LabelResult
            {
                Data = new byte[] { 0x25, 0x50, 0x44, 0x46 },
                Format = LabelFormat.Pdf,
                FileName = "label.pdf"
            });
        var logger = new Mock<ILogger<GetShipmentLabelHandler>>();
        var sut = new GetShipmentLabelHandler(new[] { adapter.Object }, logger.Object);
        var result = await sut.Handle(new GetShipmentLabelQuery(Guid.NewGuid(), "SHP-001"), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        adapter.Verify(a => a.GetShipmentLabelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
