using FluentAssertions;
using MesTech.Application.Features.Orders.Queries.GetStaleOrders;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetStaleOrdersHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new GetStaleOrdersQueryHandler(
            Mock.Of<IOrderRepository>(), Mock.Of<ILogger<GetStaleOrdersQueryHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
