using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetCommissionSummaryHandler tests — platform komisyon ozeti, toplam hesaplama ve bos senaryo.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetCommissionSummaryHandlerTests
{
    private readonly Mock<ICommissionRecordRepository> _repoMock;
    private readonly GetCommissionSummaryHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCommissionSummaryHandlerTests()
    {
        _repoMock = new Mock<ICommissionRecordRepository>();
        _sut = new GetCommissionSummaryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_WithRecords_ReturnsSummaryWithPlatformBreakdown()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);
        var query = new GetCommissionSummaryQuery(_tenantId, from, to);

        var trendyolRecords = new List<CommissionRecord>
        {
            CommissionRecord.Create(_tenantId, "Trendyol", 1000m, 0.15m, 150m, 10m, "ORD-001"),
            CommissionRecord.Create(_tenantId, "Trendyol", 2000m, 0.15m, 300m, 20m, "ORD-002")
        };

        var hbRecords = new List<CommissionRecord>
        {
            CommissionRecord.Create(_tenantId, "Hepsiburada", 500m, 0.12m, 60m, 5m, "ORD-003")
        };

        _repoMock
            .Setup(r => r.GetByPlatformAsync(_tenantId, "Trendyol", from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trendyolRecords.AsReadOnly());

        _repoMock
            .Setup(r => r.GetByPlatformAsync(_tenantId, "Hepsiburada", from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hbRecords.AsReadOnly());

        // Return empty for other platforms
        foreach (var platform in new[] { "N11", "Ciceksepeti", "Amazon", "Pazarama" })
        {
            _repoMock
                .Setup(r => r.GetByPlatformAsync(_tenantId, platform, from, to, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CommissionRecord>().AsReadOnly());
        }

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCommission.Should().Be(510m); // 150 + 300 + 60
        result.TotalServiceFee.Should().Be(35m); // 10 + 20 + 5
        result.ByPlatform.Should().HaveCount(2);

        var trendyol = result.ByPlatform.First(p => p.Platform == "Trendyol");
        trendyol.TotalCommission.Should().Be(450m);
        trendyol.TotalGross.Should().Be(3000m);
        trendyol.RecordCount.Should().Be(2);
        trendyol.AverageRate.Should().Be(15m); // (450/3000)*100 = 15

        var hb = result.ByPlatform.First(p => p.Platform == "Hepsiburada");
        hb.TotalCommission.Should().Be(60m);
        hb.RecordCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoRecordsForAnyPlatform_ReturnsZeroSummary()
    {
        // Arrange
        var from = new DateTime(2026, 6, 1);
        var to = new DateTime(2026, 6, 30);
        var query = new GetCommissionSummaryQuery(_tenantId, from, to);

        foreach (var platform in new[] { "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama" })
        {
            _repoMock
                .Setup(r => r.GetByPlatformAsync(_tenantId, platform, from, to, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CommissionRecord>().AsReadOnly());
        }

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCommission.Should().Be(0m);
        result.TotalServiceFee.Should().Be(0m);
        result.ByPlatform.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ZeroGrossAmount_AverageRateIsZero()
    {
        // Arrange
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 31);
        var query = new GetCommissionSummaryQuery(_tenantId, from, to);

        // Edge case: gross=0, commission=0 (e.g., free promotional item)
        var records = new List<CommissionRecord>
        {
            CommissionRecord.Create(_tenantId, "N11", 0m, 0m, 0m, 0m, "ORD-PROMO")
        };

        _repoMock
            .Setup(r => r.GetByPlatformAsync(_tenantId, "N11", from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(records.AsReadOnly());

        foreach (var platform in new[] { "Trendyol", "Hepsiburada", "Ciceksepeti", "Amazon", "Pazarama" })
        {
            _repoMock
                .Setup(r => r.GetByPlatformAsync(_tenantId, platform, from, to, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CommissionRecord>().AsReadOnly());
        }

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ByPlatform.Should().HaveCount(1);
        result.ByPlatform[0].AverageRate.Should().Be(0m); // division by zero guard
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
