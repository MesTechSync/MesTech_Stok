using FluentAssertions;
using MesTech.Application.Features.Health.Queries.GetHealthStatus;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetHealthStatusHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsHealthDto()
    {
        var sut = new GetHealthStatusHandler(Mock.Of<ICacheService>());
        var query = new GetHealthStatusQuery();
        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }
}
