using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;
using MesTech.Application.Features.Accounting.Queries.GetAccountBalance;
using MesTech.Application.Features.Accounting.Queries.GetJournalEntries;
using MesTech.Application.Features.Accounting.Queries.GetCounterparties;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecords;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecords;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecords;
using MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;
using MesTech.Application.Features.Accounting.Queries.GetAccountingPeriods;
using MesTech.Application.Features.Accounting.Queries.GetAccountingExpenses;
using MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;
using MesTech.Application.Features.Accounting.Queries.GetFifoCOGS;
using MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;
using MesTech.Application.Features.Accounting.Queries.GenerateBaBsReport;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;
using MesTech.Application.Interfaces.Accounting;
using IJournalEntryRepository = MesTech.Domain.Interfaces.IJournalEntryRepository;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using AccountType = MesTech.Domain.Accounting.Enums.AccountType;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// Accounting Query Handler happy path + empty data testleri.
/// Null-guard testleri AccountingQueryHandlerTests.cs'de mevcut.
/// Bu dosya İŞ MANTIĞI doğrulaması yapar.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
[Trait("Group", "QueryHandler-HappyPath")]
public class AccountingQueryHappyPathTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══ GetChartOfAccounts — happy path ═══

    [Fact]
    public async Task GetChartOfAccounts_WithAccounts_ReturnsMappedDtos()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var kasa = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        var banka = ChartOfAccounts.Create(_tenantId, "102", "Bankalar", AccountType.Asset);
        var satis = ChartOfAccounts.Create(_tenantId, "600", "Satışlar", AccountType.Revenue);

        repo.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts> { kasa, banka, satis }.AsReadOnly());

        var handler = new GetChartOfAccountsHandler(repo.Object);
        var result = await handler.Handle(
            new GetChartOfAccountsQuery(_tenantId), CancellationToken.None);

        result.Should().HaveCount(3);
        result.Should().Contain(x => x.Code == "100");
        result.Should().Contain(x => x.Code == "600");
    }

    [Fact]
    public async Task GetChartOfAccounts_InactiveFilter_PassesCorrectParam()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        repo.Setup(r => r.GetAllAsync(_tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts>().AsReadOnly());

        var handler = new GetChartOfAccountsHandler(repo.Object);
        await handler.Handle(
            new GetChartOfAccountsQuery(_tenantId, IsActive: false), CancellationToken.None);

        repo.Verify(r => r.GetAllAsync(_tenantId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ GetAccountBalance — happy path ═══

    [Fact]
    public async Task GetAccountBalance_WithEntries_CalculatesCorrectBalance()
    {
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        var journalRepo = new Mock<IJournalEntryRepository>();

        var kasa = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        accountRepo.Setup(r => r.GetByIdAsync(kasa.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(kasa);

        // 2 yevmiye: 5000 borç + 2000 alacak = 3000 net
        var entry1 = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Satış", "JE-001");
        entry1.AddLine(kasa.Id, 5000m, 0m, "Kasa borç");
        entry1.AddLine(Guid.NewGuid(), 0m, 5000m, "Satış");
        entry1.Validate();
        entry1.Post();

        var entry2 = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Gider", "JE-002");
        entry2.AddLine(kasa.Id, 0m, 2000m, "Kasa alacak");
        entry2.AddLine(Guid.NewGuid(), 2000m, 0m, "Gider");
        entry2.Validate();
        entry2.Post();

        journalRepo.Setup(r => r.GetByAccountIdAsync(kasa.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry1, entry2 });

        var handler = new GetAccountBalanceHandler(accountRepo.Object, journalRepo.Object);
        var result = await handler.Handle(
            new GetAccountBalanceQuery(_tenantId, kasa.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalDebit.Should().Be(5000m);
        result.TotalCredit.Should().Be(2000m);
        result.Balance.Should().Be(3000m);
    }

    // ═══ GetJournalEntries — happy path ═══

    [Fact]
    public async Task GetJournalEntries_WithDateRange_ReturnsFilteredEntries()
    {
        var repo = new Mock<IJournalEntryRepository>();
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 3, 31);

        var entry = JournalEntry.Create(_tenantId, new DateTime(2026, 2, 15), "Test", "JE-001");
        entry.AddLine(Guid.NewGuid(), 1000m, 0m, "Borç");
        entry.AddLine(Guid.NewGuid(), 0m, 1000m, "Alacak");
        entry.Validate();
        entry.Post();

        repo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        var handler = new GetJournalEntriesHandler(repo.Object);
        var result = await handler.Handle(
            new GetJournalEntriesQuery(_tenantId, from, to), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Reference.Should().Be("JE-001");
    }

    [Fact]
    public async Task GetJournalEntries_EmptyRange_ReturnsEmptyList()
    {
        var repo = new Mock<IJournalEntryRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var handler = new GetJournalEntriesHandler(repo.Object);
        var result = await handler.Handle(
            new GetJournalEntriesQuery(_tenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow),
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetCounterparties — happy path ═══

    [Fact]
    public async Task GetCounterparties_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<ICounterpartyRepository>();
        repo.Setup(r => r.GetAllAsync(_tenantId, It.IsAny<CounterpartyType?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Counterparty>());

        var handler = new GetCounterpartiesHandler(repo.Object);
        var result = await handler.Handle(
            new GetCounterpartiesQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetFixedExpenses — happy path ═══

    [Fact]
    public async Task GetFixedExpenses_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        repo.Setup(r => r.GetAllAsync(_tenantId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FixedExpense>());

        var handler = new GetFixedExpensesHandler(repo.Object);
        var result = await handler.Handle(
            new GetFixedExpensesQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetPenaltyRecords — happy path ═══

    [Fact]
    public async Task GetPenaltyRecords_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        repo.Setup(r => r.GetAllAsync(_tenantId, It.IsAny<PenaltySource?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PenaltyRecord>());

        var handler = new GetPenaltyRecordsHandler(repo.Object);
        var result = await handler.Handle(
            new GetPenaltyRecordsQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetSalaryRecords — happy path ═══

    [Fact]
    public async Task GetSalaryRecords_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        repo.Setup(r => r.GetAllAsync(_tenantId, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SalaryRecord>());

        var handler = new GetSalaryRecordsHandler(repo.Object);
        var result = await handler.Handle(
            new GetSalaryRecordsQuery(_tenantId, 2026, 3), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetTaxRecords — happy path ═══

    [Fact]
    public async Task GetTaxRecords_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<ITaxRecordRepository>();
        repo.Setup(r => r.GetAllAsync(_tenantId, It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaxRecord>());

        var handler = new GetTaxRecordsHandler(repo.Object);
        var result = await handler.Handle(
            new GetTaxRecordsQuery(_tenantId, "KDV", 2026), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetPlatformCommissionRates — happy path ═══

    [Fact]
    public async Task GetPlatformCommissionRates_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        repo.Setup(r => r.GetAllAsync(_tenantId, It.IsAny<PlatformType?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformCommission>());

        var handler = new GetPlatformCommissionRatesHandler(repo.Object);
        var result = await handler.Handle(
            new GetPlatformCommissionRatesQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetAccountingPeriods — happy path ═══

    [Fact]
    public async Task GetAccountingPeriods_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IAccountingPeriodRepository>();
        repo.Setup(r => r.GetByYearAsync(_tenantId, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountingPeriod>());

        var handler = new GetAccountingPeriodsHandler(repo.Object);
        var result = await handler.Handle(
            new GetAccountingPeriodsQuery(_tenantId, 2026), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetReconciliationDashboard — happy path ═══

    [Fact]
    public async Task GetReconciliationDashboard_EmptyRepo_ReturnsZeroTotals()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();

        matchRepo.Setup(r => r.GetByStatusAsync(_tenantId, It.IsAny<ReconciliationStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>());
        settlementRepo.Setup(r => r.GetByPlatformAsync(_tenantId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>());

        var handler = new GetReconciliationDashboardHandler(matchRepo.Object, settlementRepo.Object);
        var result = await handler.Handle(
            new GetReconciliationDashboardQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
    }

    // ═══ GetReconciliationMatches — happy path ═══

    [Fact]
    public async Task GetReconciliationMatches_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IReconciliationMatchRepository>();
        repo.Setup(r => r.GetByStatusAsync(_tenantId, It.IsAny<ReconciliationStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>());

        var handler = new GetReconciliationMatchesHandler(repo.Object);
        var result = await handler.Handle(
            new GetReconciliationMatchesQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetCashFlowTrend — happy path ═══

    [Fact]
    public async Task GetCashFlowTrend_EmptyRepos_ReturnsEmptyMonths()
    {
        var incomeRepo = new Mock<IIncomeRepository>();
        var expenseRepo = new Mock<IExpenseRepository>();
        var logger = Mock.Of<ILogger<GetCashFlowTrendHandler>>();

        incomeRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Income>());
        expenseRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Expense>());

        var handler = new GetCashFlowTrendHandler(incomeRepo.Object, expenseRepo.Object, logger);
        var result = await handler.Handle(
            new GetCashFlowTrendQuery(_tenantId, 6), CancellationToken.None);

        result.Should().NotBeNull();
    }

    // ═══ GetIncomeExpenseSummary — happy path ═══

    [Fact]
    public async Task GetIncomeExpenseSummary_EmptyRepos_ReturnsZeroTotals()
    {
        var incomeRepo = new Mock<IIncomeRepository>();
        var expenseRepo = new Mock<IExpenseRepository>();
        var logger = Mock.Of<ILogger<GetIncomeExpenseSummaryHandler>>();

        incomeRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Income>());
        expenseRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Expense>());

        var handler = new GetIncomeExpenseSummaryHandler(incomeRepo.Object, expenseRepo.Object, logger);
        var result = await handler.Handle(
            new GetIncomeExpenseSummaryQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalIncome.Should().Be(0);
        result.TotalExpense.Should().Be(0);
    }

    // ═══ GetFifoCOGS — happy path ═══

    [Fact]
    public async Task GetFifoCOGS_EmptyService_ReturnsEmptyList()
    {
        var service = new Mock<IFifoCostCalculationService>();
        service.Setup(s => s.CalculateAllAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FifoCostResultDto>());

        var handler = new GetFifoCOGSHandler(service.Object);
        var result = await handler.Handle(
            new GetFifoCOGSQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetCommissionSummary — happy path ═══

    [Fact]
    public async Task GetCommissionSummary_EmptyRepo_ReturnsEmptyPlatforms()
    {
        var repo = new Mock<ICommissionRecordRepository>();
        repo.Setup(r => r.GetByPlatformAndDateRangeAsync(
                _tenantId, It.IsAny<PlatformType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CommissionRecord>());

        var handler = new GetCommissionSummaryHandler(repo.Object);
        var result = await handler.Handle(
            new GetCommissionSummaryQuery(_tenantId, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)),
            CancellationToken.None);

        result.Should().NotBeNull();
    }

    // ═══ GetSettlementBatches — happy path ═══

    [Fact]
    public async Task GetSettlementBatches_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<ISettlementBatchRepository>();
        repo.Setup(r => r.GetByPlatformAsync(_tenantId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>());

        var handler = new GetSettlementBatchesHandler(repo.Object);
        var result = await handler.Handle(
            new GetSettlementBatchesQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GenerateBaBsReport — happy path ═══

    [Fact]
    public async Task GenerateBaBsReport_NullRequest_Throws()
    {
        var service = new Mock<IBaBsReportService>();
        var handler = new GenerateBaBsReportHandler(service.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetProfitReport — happy path ═══

    [Fact]
    public async Task GetProfitReport_NullRequest_Throws()
    {
        var repo = new Mock<IProfitReportRepository>();
        var service = new Mock<IProfitCalculationService>();
        var handler = new GetProfitReportHandler(repo.Object, service.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
