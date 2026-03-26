using FluentAssertions;
using MesTech.Application.DTOs.Dashboard;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetDashboardSummaryHandlerTests
{
    private readonly Mock<IDashboardSummaryRepository> _repoMock = new();
    private readonly GetDashboardSummaryQueryHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetDashboardSummaryHandlerTests()
    {
        _sut = new GetDashboardSummaryQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsSummaryDto()
    {
        var dto = new DashboardSummaryDto
        {
            TotalProducts = 150,
            TotalOrders = 42,
            LowStockCount = 8,
            TodayRevenue = 12500m
        };

        _repoMock.Setup(r => r.GetSummaryAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var query = new GetDashboardSummaryQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalProducts.Should().Be(150);
        result.TotalOrders.Should().Be(42);
        result.LowStockCount.Should().Be(8);
        result.TodayRevenue.Should().Be(12500m);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
