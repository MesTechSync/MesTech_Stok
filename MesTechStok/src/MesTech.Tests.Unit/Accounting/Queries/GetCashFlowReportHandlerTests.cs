using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetCashFlowReportHandler tests — nakit akisi raporu, inflow/outflow toplami ve DTO mapping.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetCashFlowReportHandlerTests
{
    private readonly Mock<ICashFlowEntryRepository> _repoMock;
    private readonly GetCashFlowReportHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCashFlowReportHandlerTests()
    {
        _repoMock = new Mock<ICashFlowEntryRepository>();
        _sut = new GetCashFlowReportHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidDateRange_ReturnsReportWithCorrectTotals()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);
        var query = new GetCashFlowReportQuery(_tenantId, from, to);

        var entries = new List<CashFlowEntry>
        {
            CashFlowEntry.Create(_tenantId, new DateTime(2026, 1, 5), 10000m, CashFlowDirection.Inflow, "Satis", "Trendyol hasilat"),
            CashFlowEntry.Create(_tenantId, new DateTime(2026, 1, 10), 3000m, CashFlowDirection.Outflow, "Kira", "Ofis kirasi"),
            CashFlowEntry.Create(_tenantId, new DateTime(2026, 1, 20), 5000m, CashFlowDirection.Inflow, "Satis", "HB hasilat")
        };

        _repoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries.AsReadOnly());

        _repoMock
            .Setup(r => r.GetTotalByDirectionAsync(_tenantId, CashFlowDirection.Inflow, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15000m);

        _repoMock
            .Setup(r => r.GetTotalByDirectionAsync(_tenantId, CashFlowDirection.Outflow, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3000m);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalInflow.Should().Be(15000m);
        result.TotalOutflow.Should().Be(3000m);
        result.NetFlow.Should().Be(12000m);
        result.Entries.Should().HaveCount(3);
        result.Entries[0].Direction.Should().Be(CashFlowDirection.Inflow.ToString());
        result.Entries[0].Amount.Should().Be(10000m);
        result.Entries[1].Direction.Should().Be(CashFlowDirection.Outflow.ToString());
    }

    [Fact]
    public async Task Handle_NoEntries_ReturnsZeroReport()
    {
        // Arrange
        var from = new DateTime(2026, 6, 1);
        var to = new DateTime(2026, 6, 30);
        var query = new GetCashFlowReportQuery(_tenantId, from, to);

        _repoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CashFlowEntry>().AsReadOnly());

        _repoMock
            .Setup(r => r.GetTotalByDirectionAsync(_tenantId, CashFlowDirection.Inflow, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _repoMock
            .Setup(r => r.GetTotalByDirectionAsync(_tenantId, CashFlowDirection.Outflow, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalInflow.Should().Be(0m);
        result.TotalOutflow.Should().Be(0m);
        result.NetFlow.Should().Be(0m);
        result.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NegativeNetFlow_CorrectlyCalculated()
    {
        // Arrange — outflow exceeds inflow (cash deficit)
        var from = new DateTime(2026, 4, 1);
        var to = new DateTime(2026, 4, 30);
        var query = new GetCashFlowReportQuery(_tenantId, from, to);

        var entries = new List<CashFlowEntry>
        {
            CashFlowEntry.Create(_tenantId, new DateTime(2026, 4, 10), 2000m, CashFlowDirection.Inflow, "Satis"),
            CashFlowEntry.Create(_tenantId, new DateTime(2026, 4, 15), 8000m, CashFlowDirection.Outflow, "Tedarikci")
        };

        _repoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries.AsReadOnly());

        _repoMock
            .Setup(r => r.GetTotalByDirectionAsync(_tenantId, CashFlowDirection.Inflow, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2000m);

        _repoMock
            .Setup(r => r.GetTotalByDirectionAsync(_tenantId, CashFlowDirection.Outflow, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8000m);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.NetFlow.Should().Be(-6000m);
        result.TotalInflow.Should().BeLessThan(result.TotalOutflow);
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
