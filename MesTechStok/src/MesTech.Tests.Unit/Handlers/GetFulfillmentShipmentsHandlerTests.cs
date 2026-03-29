using FluentAssertions;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetFulfillmentShipmentsHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new GetFulfillmentShipmentsHandler(
            Mock.Of<IFulfillmentShipmentRepository>(),
            Mock.Of<ILogger<GetFulfillmentShipmentsHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
