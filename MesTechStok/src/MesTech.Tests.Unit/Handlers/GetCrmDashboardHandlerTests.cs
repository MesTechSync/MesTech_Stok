using FluentAssertions;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Queries.GetCrmDashboard;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetCrmDashboardHandlerTests
{
    private readonly Mock<ICrmDashboardQueryService> _queryServiceMock = new();
    private readonly GetCrmDashboardHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCrmDashboardHandlerTests()
    {
        _sut = new GetCrmDashboardHandler(_queryServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsDashboardDto()
    {
        var dto = new CrmDashboardDto
        {
            TotalLeads = 42,
            OpenDeals = 15,
            TotalCustomers = 100
        };
        _queryServiceMock.Setup(s => s.GetDashboardAsync(_tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var query = new GetCrmDashboardQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalLeads.Should().Be(42);
        result.OpenDeals.Should().Be(15);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
