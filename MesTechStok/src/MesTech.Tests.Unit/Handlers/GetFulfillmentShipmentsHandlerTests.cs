using FluentAssertions;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetFulfillmentShipmentsHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var mockRepository = new Mock<IOrderRepository>();
        var sut = new GetFulfillmentShipmentsHandler(mockRepository.Object);
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
