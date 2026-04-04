using FluentAssertions;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class FulfillmentQueryHandlerTests
{
    [Fact]
    public async Task GetFulfillmentDashboard_ReturnsDto()
    {
        var productRepo = new Mock<IProductRepository>();
        var shipmentRepo = new Mock<IFulfillmentShipmentRepository>();
        var logger = new Mock<ILogger<GetFulfillmentDashboardHandler>>();

        var handler = new GetFulfillmentDashboardHandler(productRepo.Object, shipmentRepo.Object, logger.Object);
        var result = await handler.Handle(
            new GetFulfillmentDashboardQuery(TenantId: Guid.NewGuid()),
            CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFulfillmentShipments_ReturnsEmptyResult()
    {
        var repo = new Mock<IFulfillmentShipmentRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MesTech.Domain.Entities.FulfillmentShipment>());
        repo.Setup(r => r.CountByTenantAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var logger = new Mock<ILogger<GetFulfillmentShipmentsHandler>>();
        var handler = new GetFulfillmentShipmentsHandler(repo.Object, logger.Object);
        var result = await handler.Handle(
            new GetFulfillmentShipmentsQuery(TenantId: Guid.NewGuid(), Page: 1, PageSize: 20),
            CancellationToken.None);
        result.Should().NotBeNull();
    }
}
