using FluentAssertions;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetFulfillmentDashboardHandlerTests
{
    private readonly GetFulfillmentDashboardHandler _sut;

    public GetFulfillmentDashboardHandlerTests()
    {
        _sut = new GetFulfillmentDashboardHandler(
            Mock.Of<IProductRepository>(),
            Mock.Of<IFulfillmentShipmentRepository>(),
            Mock.Of<ILogger<GetFulfillmentDashboardHandler>>());
    }

    [Fact]
    public async Task Handle_ReturnsDefaultDashboard()
    {
        var query = new GetFulfillmentDashboardQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalProducts.Should().BeGreaterOrEqualTo(0);
        result.PendingShipments.Should().BeGreaterOrEqualTo(0);
    }
}
