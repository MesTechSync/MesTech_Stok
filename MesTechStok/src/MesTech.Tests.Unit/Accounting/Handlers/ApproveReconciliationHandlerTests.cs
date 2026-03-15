using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// ApproveReconciliationHandler tests — approve match, mark reconciled, not found.
/// </summary>
[Trait("Category", "Unit")]
public class ApproveReconciliationHandlerTests
{
    private readonly Mock<IReconciliationMatchRepository> _matchRepoMock;
    private readonly Mock<IBankTransactionRepository> _bankTxRepoMock;
    private readonly Mock<ISettlementBatchRepository> _settlementRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly ApproveReconciliationHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ApproveReconciliationHandlerTests()
    {
        _matchRepoMock = new Mock<IReconciliationMatchRepository>();
        _bankTxRepoMock = new Mock<IBankTransactionRepository>();
        _settlementRepoMock = new Mock<ISettlementBatchRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _sut = new ApproveReconciliationHandler(
            _matchRepoMock.Object,
            _bankTxRepoMock.Object,
            _settlementRepoMock.Object,
            _uowMock.Object);
    }

    private ReconciliationMatch CreateNeedsReviewMatch()
    {
        var batchId = Guid.NewGuid();
        var txId = Guid.NewGuid();

        return ReconciliationMatch.Create(
            _tenantId,
            DateTime.UtcNow,
            0.80m,
            ReconciliationStatus.NeedsReview,
            batchId,
            txId);
    }

    [Fact]
    public async Task Handle_ValidMatch_SetsManualMatch()
    {
        var match = CreateNeedsReviewMatch();
        var reviewedBy = Guid.NewGuid();

        _matchRepoMock
            .Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _bankTxRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BankTransaction.Create(
                _tenantId, Guid.NewGuid(), DateTime.UtcNow, 1000m, "Test"));

        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SettlementBatch.Create(
                _tenantId, "Trendyol", DateTime.UtcNow, DateTime.UtcNow, 1000m, 100m, 900m));

        var command = new ApproveReconciliationCommand(match.Id, reviewedBy);
        await _sut.Handle(command, CancellationToken.None);

        match.Status.Should().Be(ReconciliationStatus.ManualMatch);
        match.ReviewedBy.Should().Be(reviewedBy.ToString());
        match.ReviewedAt.Should().NotBeNull();

        _matchRepoMock.Verify(
            r => r.UpdateAsync(match, It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NonExistent_ThrowsInvalidOperation()
    {
        var matchId = Guid.NewGuid();

        _matchRepoMock
            .Setup(r => r.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var command = new ApproveReconciliationCommand(matchId, Guid.NewGuid());

        var act = async () => await _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{matchId}*not found*");
    }

    [Fact]
    public async Task Handle_MarksBankTransactionReconciled()
    {
        var match = CreateNeedsReviewMatch();
        var bankTx = BankTransaction.Create(
            _tenantId, Guid.NewGuid(), DateTime.UtcNow, 1000m, "Test");

        _matchRepoMock
            .Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _bankTxRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bankTx);

        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementBatch?)null);

        await _sut.Handle(new ApproveReconciliationCommand(match.Id, Guid.NewGuid()), CancellationToken.None);

        bankTx.IsReconciled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MarksSettlementBatchReconciled()
    {
        var match = CreateNeedsReviewMatch();
        var batch = SettlementBatch.Create(
            _tenantId, "Trendyol", DateTime.UtcNow, DateTime.UtcNow, 1000m, 100m, 900m);

        _matchRepoMock
            .Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _bankTxRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankTransaction?)null);

        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        await _sut.Handle(new ApproveReconciliationCommand(match.Id, Guid.NewGuid()), CancellationToken.None);

        batch.Status.Should().Be(SettlementStatus.Reconciled);
    }

    [Fact]
    public async Task Handle_NullBankTx_DoesNotThrow()
    {
        var match = CreateNeedsReviewMatch();

        _matchRepoMock
            .Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _bankTxRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankTransaction?)null);

        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementBatch?)null);

        var act = async () => await _sut.Handle(
            new ApproveReconciliationCommand(match.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
