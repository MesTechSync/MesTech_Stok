using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;
using MesTech.Application.Features.Accounting.Queries.GetAccountBalance;
using MesTech.Application.Features.Accounting.Queries.GetTrialBalance;
using MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;
using MesTech.Application.Features.Accounting.Queries.GetJournalEntries;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecords;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecordById;
using MesTech.Application.Features.Accounting.Queries.GetTaxSummary;
using MesTech.Application.Features.Accounting.Queries.GetCounterparties;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenseById;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecords;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecordById;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecords;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecordById;
using MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;
using MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;
using MesTech.Application.Features.Accounting.Queries.GetAccountingExpenses;
using MesTech.Application.Features.Accounting.Queries.GetAccountingPeriods;
using MesTech.Application.Features.Accounting.Queries.GetBankTransactions;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;
using MesTech.Application.Features.Accounting.Queries.GetCargoComparison;
using MesTech.Application.Features.Accounting.Queries.GetFifoCOGS;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;
using MesTech.Application.Features.Accounting.Queries.GetKdvReport;
using MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;
using MesTech.Application.Features.Accounting.Queries.GetPendingReviews;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;
using MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;
using MesTech.Application.Features.Accounting.Queries.GetShipmentCosts;
using MesTech.Application.Features.Accounting.Queries.GetWithholdingRates;
using MesTech.Application.Features.Accounting.Queries.ListFixedAssets;
using MesTech.Application.Features.Accounting.Queries.ListTaxWithholdings;
using MesTech.Application.Features.Accounting.Queries.ValidateBalanceSheet;
using MesTech.Application.Features.Accounting.Queries.ValidateTrialBalance;
using MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;
using MesTech.Application.Features.Accounting.Queries.GenerateBaBsReport;
using MesTech.Application.Interfaces.Accounting;
using IJournalEntryRepository = MesTech.Domain.Interfaces.IJournalEntryRepository;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// Accounting Query Handler testleri.
/// Her handler: null request → exception, empty data → boş/null sonuç.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
[Trait("Group", "QueryHandler")]
public class AccountingQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══ GetChartOfAccounts ═══

    [Fact]
    public async Task GetChartOfAccounts_NullRequest_Throws()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var handler = new GetChartOfAccountsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetChartOfAccounts_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        repo.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts>().AsReadOnly());

        var handler = new GetChartOfAccountsHandler(repo.Object);
        var result = await handler.Handle(new GetChartOfAccountsQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══ GetAccountBalance ═══

    [Fact]
    public async Task GetAccountBalance_NullRequest_Throws()
    {
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        var journalRepo = new Mock<IJournalEntryRepository>();
        var handler = new GetAccountBalanceHandler(accountRepo.Object, journalRepo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetAccountBalance_AccountNotFound_ReturnsNull()
    {
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);
        var journalRepo = new Mock<IJournalEntryRepository>();

        var handler = new GetAccountBalanceHandler(accountRepo.Object, journalRepo.Object);
        var result = await handler.Handle(
            new GetAccountBalanceQuery(_tenantId, Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    // ═══ GetTrialBalance ═══

    [Fact]
    public async Task GetTrialBalance_NullRequest_Throws()
    {
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        var journalRepo = new Mock<IJournalEntryRepository>();
        var handler = new GetTrialBalanceHandler(accountRepo.Object, journalRepo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetJournalEntries ═══

    [Fact]
    public async Task GetJournalEntries_NullRequest_Throws()
    {
        var repo = new Mock<IJournalEntryRepository>();
        var handler = new GetJournalEntriesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetTaxRecords ═══

    [Fact]
    public async Task GetTaxRecords_NullRequest_Throws()
    {
        var repo = new Mock<ITaxRecordRepository>();
        var handler = new GetTaxRecordsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetTaxRecordById ═══

    [Fact]
    public async Task GetTaxRecordById_NullRequest_Throws()
    {
        var repo = new Mock<ITaxRecordRepository>();
        var handler = new GetTaxRecordByIdHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetCounterparties ═══

    [Fact]
    public async Task GetCounterparties_NullRequest_Throws()
    {
        var repo = new Mock<ICounterpartyRepository>();
        var handler = new GetCounterpartiesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetFixedExpenses ═══

    [Fact]
    public async Task GetFixedExpenses_NullRequest_Throws()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var handler = new GetFixedExpensesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetFixedExpenseById ═══

    [Fact]
    public async Task GetFixedExpenseById_NullRequest_Throws()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var handler = new GetFixedExpenseByIdHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetPenaltyRecords ═══

    [Fact]
    public async Task GetPenaltyRecords_NullRequest_Throws()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var handler = new GetPenaltyRecordsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetPenaltyRecordById ═══

    [Fact]
    public async Task GetPenaltyRecordById_NullRequest_Throws()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var handler = new GetPenaltyRecordByIdHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetSalaryRecords ═══

    [Fact]
    public async Task GetSalaryRecords_NullRequest_Throws()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        var handler = new GetSalaryRecordsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetSalaryRecordById ═══

    [Fact]
    public async Task GetSalaryRecordById_NullRequest_Throws()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        var handler = new GetSalaryRecordByIdHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetPlatformCommissionRates ═══

    [Fact]
    public async Task GetPlatformCommissionRates_NullRequest_Throws()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        var handler = new GetPlatformCommissionRatesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetCommissionSummary ═══

    [Fact]
    public async Task GetCommissionSummary_NullRequest_Throws()
    {
        var repo = new Mock<ICommissionRecordRepository>();
        var handler = new GetCommissionSummaryHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetAccountingExpenses ═══

    [Fact]
    public async Task GetAccountingExpenses_NullRequest_Throws()
    {
        var repo = new Mock<IPersonalExpenseRepository>();
        var handler = new GetAccountingExpensesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetBankTransactions ═══

    [Fact]
    public async Task GetBankTransactions_NullRequest_Throws()
    {
        var repo = new Mock<IBankTransactionRepository>();
        var handler = new GetBankTransactionsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetSettlementBatches ═══

    [Fact]
    public async Task GetSettlementBatches_NullRequest_Throws()
    {
        var repo = new Mock<ISettlementBatchRepository>();
        var handler = new GetSettlementBatchesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetReconciliationMatches ═══

    [Fact]
    public async Task GetReconciliationMatches_NullRequest_Throws()
    {
        var repo = new Mock<IReconciliationMatchRepository>();
        var handler = new GetReconciliationMatchesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetShipmentCosts ═══

    [Fact]
    public async Task GetShipmentCosts_NullRequest_Throws()
    {
        var repo = new Mock<IShipmentCostRepository>();
        var handler = new GetShipmentCostsHandler(repo.Object, Mock.Of<ILogger<GetShipmentCostsHandler>>());
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetWithholdingRates ═══

    [Fact]
    public async Task GetWithholdingRates_NullRequest_Throws()
    {
        var handler = new GetWithholdingRatesHandler();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ ListFixedAssets ═══

    [Fact]
    public async Task ListFixedAssets_NullRequest_Throws()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var handler = new ListFixedAssetsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ ListTaxWithholdings ═══

    [Fact]
    public async Task ListTaxWithholdings_NullRequest_Throws()
    {
        var repo = new Mock<ITaxWithholdingRepository>();
        var handler = new ListTaxWithholdingsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
