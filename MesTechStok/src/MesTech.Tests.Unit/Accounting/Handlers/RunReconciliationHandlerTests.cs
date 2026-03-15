using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RunReconciliation;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// RunReconciliationHandler tests — auto-matching, review, unmatched scenarios.
/// </summary>
[Trait("Category", "Unit")]
public class RunReconciliationHandlerTests
{
    private readonly Mock<ISettlementBatchRepository> _settlementRepoMock;
    private readonly Mock<IBankTransactionRepository> _bankTxRepoMock;
    private readonly Mock<IReconciliationMatchRepository> _matchRepoMock;
    private readonly Mock<IReconciliationScoringService> _scoringServiceMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly RunReconciliationHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RunReconciliationHandlerTests()
    {
        _settlementRepoMock = new Mock<ISettlementBatchRepository>();
        _bankTxRepoMock = new Mock<IBankTransactionRepository>();
        _matchRepoMock = new Mock<IReconciliationMatchRepository>();
        _scoringServiceMock = new Mock<IReconciliationScoringService>();
        _uowMock = new Mock<IUnitOfWork>();

        _scoringServiceMock.Setup(s => s.AutoMatchThreshold).Returns(0.85m);
        _scoringServiceMock.Setup(s => s.ReviewThreshold).Returns(0.70m);

        _sut = new RunReconciliationHandler(
            _settlementRepoMock.Object,
            _bankTxRepoMock.Object,
            _matchRepoMock.Object,
            _scoringServiceMock.Object,
            _uowMock.Object);
    }

    private SettlementBatch CreateBatch(decimal totalNet, string platform = "Trendyol")
    {
        return SettlementBatch.Create(
            _tenantId, platform,
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
            totalNet + 100m, 100m, totalNet);
    }

    private BankTransaction CreateBankTx(decimal amount, string description = "ODEME")
    {
        return BankTransaction.Create(
            _tenantId, Guid.NewGuid(),
            DateTime.UtcNow, amount, description);
    }

    [Fact]
    public async Task Handle_WithMatchingPairs_CreatesAutoMatched()
    {
        var batch = CreateBatch(1000m);
        var tx = CreateBankTx(1000m, "TRENDYOL ODEME");

        _settlementRepoMock
            .Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        _bankTxRepoMock
            .Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction> { tx });

        _scoringServiceMock
            .Setup(s => s.CalculateConfidence(
                It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(0.95m);

        var command = new RunReconciliationCommand(_tenantId);
        var result = await _sut.Handle(command, CancellationToken.None);

        result.AutoMatchedCount.Should().Be(1);
        result.NeedsReviewCount.Should().Be(0);
        result.UnmatchedCount.Should().Be(0);
        result.AutoMatchedTotal.Should().Be(1000m);

        _matchRepoMock.Verify(
            r => r.AddAsync(It.IsAny<ReconciliationMatch>(), It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_WithPartialMatch_CreatesNeedsReview()
    {
        var batch = CreateBatch(1000m);
        var tx = CreateBankTx(1050m, "BILINMEYEN");

        _settlementRepoMock
            .Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        _bankTxRepoMock
            .Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction> { tx });

        _scoringServiceMock
            .Setup(s => s.CalculateConfidence(
                It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(0.78m);

        var command = new RunReconciliationCommand(_tenantId);
        var result = await _sut.Handle(command, CancellationToken.None);

        result.AutoMatchedCount.Should().Be(0);
        result.NeedsReviewCount.Should().Be(1);
        result.NeedsReviewTotal.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_NoMatches_ReturnsZeroCounts()
    {
        var batch = CreateBatch(1000m);
        var tx = CreateBankTx(5000m, "TOTALLY DIFFERENT");

        _settlementRepoMock
            .Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        _bankTxRepoMock
            .Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction> { tx });

        _scoringServiceMock
            .Setup(s => s.CalculateConfidence(
                It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(0.20m);

        var command = new RunReconciliationCommand(_tenantId);
        var result = await _sut.Handle(command, CancellationToken.None);

        result.AutoMatchedCount.Should().Be(0);
        result.NeedsReviewCount.Should().Be(0);
        result.UnmatchedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyBatches_ReturnsZeroCounts()
    {
        _settlementRepoMock
            .Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>());

        _bankTxRepoMock
            .Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction>());

        var command = new RunReconciliationCommand(_tenantId);
        var result = await _sut.Handle(command, CancellationToken.None);

        result.AutoMatchedCount.Should().Be(0);
        result.NeedsReviewCount.Should().Be(0);
        result.UnmatchedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MultipleBatches_MatchesEachToUniqueTx()
    {
        var batch1 = CreateBatch(1000m);
        var batch2 = CreateBatch(2000m);
        var tx1 = CreateBankTx(1000m);
        var tx2 = CreateBankTx(2000m);

        _settlementRepoMock
            .Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch1, batch2 });

        _bankTxRepoMock
            .Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction> { tx1, tx2 });

        // Return high score for exact amount matches
        _scoringServiceMock
            .Setup(s => s.CalculateConfidence(
                It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(0.90m);

        var command = new RunReconciliationCommand(_tenantId);
        var result = await _sut.Handle(command, CancellationToken.None);

        result.AutoMatchedCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_AvoidDoubleMatching_SameTxNotUsedTwice()
    {
        var batch1 = CreateBatch(1000m);
        var batch2 = CreateBatch(1000m); // Same amount
        var tx = CreateBankTx(1000m); // Only one tx

        _settlementRepoMock
            .Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch1, batch2 });

        _bankTxRepoMock
            .Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction> { tx });

        _scoringServiceMock
            .Setup(s => s.CalculateConfidence(
                It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(0.90m);

        var command = new RunReconciliationCommand(_tenantId);
        var result = await _sut.Handle(command, CancellationToken.None);

        // Only 1 should match (tx used once), second batch goes unmatched
        (result.AutoMatchedCount + result.NeedsReviewCount).Should().Be(1);
        result.UnmatchedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SavesChangesOnce()
    {
        _settlementRepoMock
            .Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>());

        _bankTxRepoMock
            .Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction>());

        var command = new RunReconciliationCommand(_tenantId);
        await _sut.Handle(command, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_AutoMatchedBatch_IsMarkedReconciled()
    {
        var batch = CreateBatch(1000m);
        var tx = CreateBankTx(1000m);

        _settlementRepoMock
            .Setup(r => r.GetUnmatchedAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch> { batch });

        _bankTxRepoMock
            .Setup(r => r.GetUnreconciledAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction> { tx });

        _scoringServiceMock
            .Setup(s => s.CalculateConfidence(
                It.IsAny<decimal>(), It.IsAny<decimal>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(0.95m);

        await _sut.Handle(new RunReconciliationCommand(_tenantId), CancellationToken.None);

        _settlementRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<SettlementBatch>(), It.IsAny<CancellationToken>()),
            Times.Once());

        _bankTxRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<BankTransaction>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
