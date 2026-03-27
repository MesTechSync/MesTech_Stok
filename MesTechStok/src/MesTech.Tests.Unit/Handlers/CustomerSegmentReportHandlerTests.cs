using FluentAssertions;
using MesTech.Application.Features.Reports.CustomerSegmentReport;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CustomerSegmentReportHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly CustomerSegmentReportHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CustomerSegmentReportHandlerTests()
    {
        _sut = new CustomerSegmentReportHandler(_orderRepoMock.Object);
    }

    [Fact]
    public async Task Handle_NoOrders_ReturnsEmptyList()
    {
        _orderRepoMock.Setup(r => r.GetByDateRangeAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var query = new CustomerSegmentReportQuery(_tenantId, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
