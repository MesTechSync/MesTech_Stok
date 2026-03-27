using FluentAssertions;
using MesTech.Application.Features.Reports.CommissionReport;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CommissionReportHandlerTests
{
    private readonly Mock<ICommissionRecordRepository> _commissionRepoMock = new();
    private readonly CommissionReportHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CommissionReportHandlerTests()
    {
        _sut = new CommissionReportHandler(_commissionRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WithPlatformFilter_QueriesSinglePlatform()
    {
        _commissionRepoMock.Setup(r => r.GetByPlatformAsync(
            _tenantId, "Trendyol", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommissionRecord>().AsReadOnly());

        var query = new CommissionReportQuery(_tenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow, PlatformType.Trendyol);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        _commissionRepoMock.Verify(r => r.GetByPlatformAsync(
            _tenantId, "Trendyol", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
