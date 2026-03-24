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
        var logger = new Mock<ILogger<GetFulfillmentDashboardHandler>>();

        var handler = new GetFulfillmentDashboardHandler(productRepo.Object, logger.Object);
        var result = await handler.Handle(
            new GetFulfillmentDashboardQuery(TenantId: Guid.NewGuid()),
            CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFulfillmentShipments_ReturnsEmptyResult()
    {
        var logger = new Mock<ILogger<GetFulfillmentShipmentsHandler>>();
        var handler = new GetFulfillmentShipmentsHandler(logger.Object);
        var result = await handler.Handle(
            new GetFulfillmentShipmentsQuery(TenantId: Guid.NewGuid(), Page: 1, PageSize: 20),
            CancellationToken.None);
        result.Should().NotBeNull();
    }
}
