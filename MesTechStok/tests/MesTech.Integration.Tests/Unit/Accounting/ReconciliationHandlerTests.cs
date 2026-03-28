using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;
using MesTech.Application.Features.Accounting.Commands.RejectReconciliation;
using MesTech.Application.Features.Accounting.Commands.RunReconciliation;
using MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod;
using MesTech.Application.Features.Accounting.Commands.ImportBankStatement;
using MesTech.Application.Features.Accounting.Commands.ImportSettlement;
using MesTech.Application.Features.Accounting.Commands.UploadAccountingDocument;
using MesTech.Application.Features.Accounting.Queries.ValidateBalanceSheet;
using MesTech.Application.Features.Accounting.Queries.ValidateTrialBalance;
using MesTech.Application.Interfaces.Accounting;
using IJournalEntryRepository = MesTech.Application.Interfaces.Accounting.IJournalEntryRepository;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// Reconciliation, Period Close, Import handler testleri.
/// Kritik muhasebe iş akışları — null guard + happy path.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
[Trait("Group", "ReconciliationHandler")]
public class ReconciliationHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<IUnitOfWork> _uow = new();

    public ReconciliationHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ═══ ApproveReconciliation ═══

    [Fact]
    public async Task ApproveReconciliation_NullRequest_Throws()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var handler = new ApproveReconciliationHandler(
            matchRepo.Object, bankTxRepo.Object, settlementRepo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ApproveReconciliation_MatchNotFound_ThrowsInvalidOperation()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        matchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();

        var handler = new ApproveReconciliationHandler(
            matchRepo.Object, bankTxRepo.Object, settlementRepo.Object, _uow.Object);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(
                new ApproveReconciliationCommand(Guid.NewGuid(), _tenantId),
                CancellationToken.None));
    }

    // ═══ RejectReconciliation ═══

    [Fact]
    public async Task RejectReconciliation_NullRequest_Throws()
    {
        var repo = new Mock<IReconciliationMatchRepository>();
        var handler = new RejectReconciliationHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RejectReconciliation_MatchNotFound_ThrowsInvalidOperation()
    {
        var repo = new Mock<IReconciliationMatchRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var handler = new RejectReconciliationHandler(repo.Object, _uow.Object);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(
                new RejectReconciliationCommand(_tenantId, Guid.NewGuid(), "Reddedildi"),
                CancellationToken.None));
    }

    // ═══ RunReconciliation ═══

    [Fact]
    public async Task RunReconciliation_NullRequest_Throws()
    {
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var scoringService = new Mock<IReconciliationScoringService>();

        var handler = new RunReconciliationHandler(
            settlementRepo.Object, bankTxRepo.Object, matchRepo.Object,
            scoringService.Object, _uow.Object);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RunReconciliation_EmptyBatches_ReturnsZeroMatches()
    {
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var scoringService = new Mock<IReconciliationScoringService>();

        settlementRepo.Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>());
        bankTxRepo.Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction>());

        var handler = new RunReconciliationHandler(
            settlementRepo.Object, bankTxRepo.Object, matchRepo.Object,
            scoringService.Object, _uow.Object);

        var result = await handler.Handle(
            new RunReconciliationCommand(_tenantId), CancellationToken.None);

        result.AutoMatchedCount.Should().Be(0);
    }

    // ═══ CloseAccountingPeriod ═══

    [Fact]
    public async Task CloseAccountingPeriod_NullRequest_Throws()
    {
        var repo = new Mock<IAccountingPeriodRepository>();
        var logger = Mock.Of<ILogger<CloseAccountingPeriodHandler>>();
        var handler = new CloseAccountingPeriodHandler(repo.Object, _uow.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ ImportBankStatement ═══

    [Fact]
    public async Task ImportBankStatement_NullRequest_Throws()
    {
        var repo = new Mock<IBankTransactionRepository>();
        var handler = new ImportBankStatementHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ImportBankStatement_EmptyLines_ReturnsZero()
    {
        var repo = new Mock<IBankTransactionRepository>();
        var handler = new ImportBankStatementHandler(repo.Object, _uow.Object);

        var cmd = new ImportBankStatementCommand(
            _tenantId, Guid.NewGuid(), new List<BankTransactionInput>());

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().Be(0);
    }

    // ═══ ImportSettlement ═══

    [Fact]
    public async Task ImportSettlement_NullRequest_Throws()
    {
        var repo = new Mock<ISettlementBatchRepository>();
        var handler = new ImportSettlementHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ UploadAccountingDocument ═══

    [Fact]
    public async Task UploadAccountingDocument_NullRequest_Throws()
    {
        var repo = new Mock<IAccountingDocumentRepository>();
        var handler = new UploadAccountingDocumentHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ ValidateBalanceSheet ═══

    [Fact]
    public async Task ValidateBalanceSheet_NullRequest_Throws()
    {
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        var journalRepo = new Mock<IJournalEntryRepository>();
        var service = new Mock<BalanceSheetValidationService>();

        var handler = new ValidateBalanceSheetHandler(
            accountRepo.Object, journalRepo.Object, service.Object);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ ValidateTrialBalance ═══

    [Fact]
    public async Task ValidateTrialBalance_NullRequest_Throws()
    {
        var journalRepo = new Mock<IJournalEntryRepository>();
        var service = new Mock<TrialBalanceValidationService>();

        var handler = new ValidateTrialBalanceHandler(journalRepo.Object, service.Object);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
