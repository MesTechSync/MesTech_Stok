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
            ActiveProductCount = 150,
            TodayOrderCount = 42,
            CriticalStockCount = 8,
            TodaySalesAmount = 12500m
        };

        _repoMock.Setup(r => r.GetSummaryAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var query = new GetDashboardSummaryQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.ActiveProductCount.Should().Be(150);
        result.TodayOrderCount.Should().Be(42);
        result.CriticalStockCount.Should().Be(8);
        result.TodaySalesAmount.Should().Be(12500m);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
