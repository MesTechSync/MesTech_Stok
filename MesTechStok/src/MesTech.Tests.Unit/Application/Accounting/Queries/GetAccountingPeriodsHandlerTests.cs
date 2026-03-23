using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetAccountingPeriods;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Queries;

/// <summary>
/// GetAccountingPeriodsHandler tests — retrieves accounting periods for a tenant.
/// Verifies mapping, filtering by year, and empty results.
/// </summary>
[Trait("Category", "Unit")]
public class GetAccountingPeriodsHandlerTests
{
    private readonly Mock<IAccountingPeriodRepository> _repoMock;
    private readonly GetAccountingPeriodsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetAccountingPeriodsHandlerTests()
    {
        _repoMock = new Mock<IAccountingPeriodRepository>();
        _sut = new GetAccountingPeriodsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_WithPeriods_ReturnsMappedDtos()
    {
        // Arrange
        var period1 = AccountingPeriod.Create(_tenantId, 2026, 1);
        var period2 = AccountingPeriod.Create(_tenantId, 2026, 2);
        var period3 = AccountingPeriod.Create(_tenantId, 2026, 3);
        period3.Close("admin-user");

        _repoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountingPeriod> { period1, period2, period3 });

        var query = new GetAccountingPeriodsQuery(_tenantId, 2026);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Year.Should().Be(2026);
        result[0].Month.Should().Be(1);
        result[0].IsClosed.Should().BeFalse();

        result[2].Month.Should().Be(3);
        result[2].IsClosed.Should().BeTrue();
        result[2].ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NoPeriods_ReturnsEmptyList()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, 2025, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountingPeriod>());

        var query = new GetAccountingPeriodsQuery(_tenantId, 2025);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNullYear_QueriesAllPeriods()
    {
        // Arrange
        var period2025 = AccountingPeriod.Create(_tenantId, 2025, 12);
        var period2026 = AccountingPeriod.Create(_tenantId, 2026, 1);

        _repoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountingPeriod> { period2025, period2026 });

        var query = new GetAccountingPeriodsQuery(_tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Year == 2025 && p.Month == 12);
        result.Should().Contain(p => p.Year == 2026 && p.Month == 1);
    }

    [Fact]
    public async Task Handle_PeriodsMapping_IncludesAllFields()
    {
        // Arrange
        var period = AccountingPeriod.Create(_tenantId, 2026, 6);

        _repoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountingPeriod> { period });

        var query = new GetAccountingPeriodsQuery(_tenantId, 2026);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var dto = result[0];
        dto.Id.Should().Be(period.Id);
        dto.Year.Should().Be(2026);
        dto.Month.Should().Be(6);
        dto.StartDate.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        dto.EndDate.Should().BeAfter(dto.StartDate);
        dto.IsClosed.Should().BeFalse();
        dto.ClosedAt.Should().BeNull();
    }
}
