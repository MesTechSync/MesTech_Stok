using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Features.Dashboard;

[Trait("Category", "Unit")]
public class GetServiceHealthHandlerTests
{
    private readonly Mock<IInfrastructureHealthService> _healthMock = new();
    private readonly GetServiceHealthHandler _sut;

    public GetServiceHealthHandlerTests()
        => _sut = new GetServiceHealthHandler(_healthMock.Object);

    [Fact]
    public async Task Handle_ReturnsAllServiceStatuses()
    {
        var results = new List<HealthCheckResult>
        {
            new("PostgreSQL", true, "12ms"),
            new("Redis", true, "3ms"),
            new("RabbitMQ", false, null),
        };
        _healthMock.Setup(h => h.CheckAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        var result = await _sut.Handle(new GetServiceHealthQuery(), CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].ServiceName.Should().Be("PostgreSQL");
        result[0].IsHealthy.Should().BeTrue();
        result[2].IsHealthy.Should().BeFalse();
        result[2].ResponseTime.Should().Be("—");
    }

    [Fact]
    public async Task Handle_NoServices_ReturnsEmpty()
    {
        _healthMock.Setup(h => h.CheckAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HealthCheckResult>());

        var result = await _sut.Handle(new GetServiceHealthQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
