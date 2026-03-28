using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Finance;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class BankReconciliationReportServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<IBankTransactionRepository> _bankTxRepoMock = new();
    private readonly Mock<IJournalEntryRepository> _journalRepoMock = new();
    private readonly Mock<IChartOfAccountsRepository> _chartRepoMock = new();
    private readonly Mock<ILogger<BankReconciliationReportService>> _loggerMock = new();
    private readonly BankReconciliationReportService _sut;

    private readonly DateTime _startDate = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _endDate = new(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc);

    public BankReconciliationReportServiceTests()
    {
        _sut = new BankReconciliationReportService(
            _bankTxRepoMock.Object,
            _journalRepoMock.Object,
            _chartRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateReport_NoData_ShouldReturnEmptyReport()
    {
        SetupEmptyRepos();

        var result = await _sut.GenerateReportAsync(_tenantId, _startDate, _endDate);

        result.MatchedItems.Should().BeEmpty();
        result.UnmatchedBankItems.Should().BeEmpty();
        result.UnmatchedAccountingItems.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateReport_EmptyTenantId_ShouldThrow()
    {
        var act = () => _sut.GenerateReportAsync(Guid.Empty, _startDate, _endDate);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateReport_EndBeforeStart_ShouldThrow()
    {
        var act = () => _sut.GenerateReportAsync(_tenantId, _endDate, _startDate);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateReport_ShouldSetDates()
    {
        SetupEmptyRepos();

        var result = await _sut.GenerateReportAsync(_tenantId, _startDate, _endDate);

        result.TenantId.Should().Be(_tenantId);
        result.StartDate.Should().Be(_startDate);
        result.EndDate.Should().Be(_endDate);
    }

    // ── Setup Helpers ──

    private void SetupEmptyRepos()
    {
        _bankTxRepoMock.Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BankTransaction>());
        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, _startDate, _endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JournalEntry>());
        _chartRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ChartOfAccounts>());
    }
}
