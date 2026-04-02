using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;
using MesTech.Application.Features.Accounting.Commands.ImportBankStatement;
using MesTech.Application.Features.Accounting.Commands.RejectReconciliation;
using MesTech.Application.Features.Accounting.Commands.RunReconciliation;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

/// <summary>
/// Accounting command handler testleri — G28 kapsamı.
/// [PAYLAŞIM-DEV5] DEV1 yazdı.
/// </summary>

#region ApproveReconciliation

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class ApproveReconciliationHandlerTests
{
    private readonly Mock<IReconciliationMatchRepository> _matchRepo = new();
    private readonly Mock<IBankTransactionRepository> _bankTxRepo = new();
    private readonly Mock<ISettlementBatchRepository> _settlementRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ApproveReconciliationHandler CreateSut() =>
        new(_matchRepo.Object, _bankTxRepo.Object, _settlementRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_MatchNotFound_ShouldThrow()
    {
        _matchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var sut = CreateSut();
        var act = () => sut.Handle(
            new ApproveReconciliationCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_ValidMatch_ShouldUpdateAndSave()
    {
        var match = ReconciliationMatch.Create(Guid.NewGuid(), DateTime.UtcNow, 0.85m, ReconciliationStatus.NeedsReview);
        _matchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var sut = CreateSut();
        await sut.Handle(new ApproveReconciliationCommand(match.Id, Guid.NewGuid()), CancellationToken.None);

        _matchRepo.Verify(r => r.UpdateAsync(match, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ImportBankStatement

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class ImportBankStatementHandlerTests
{
    private readonly Mock<IBankTransactionRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ImportBankStatementHandler CreateSut() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_EmptyList_ShouldReturnZero()
    {
        var sut = CreateSut();
        var result = await sut.Handle(
            new ImportBankStatementCommand(Guid.NewGuid(), Guid.NewGuid(), []),
            CancellationToken.None);

        result.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTransactions_ShouldImportAndSave()
    {
        _repo.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankTransaction?)null);

        var sut = CreateSut();
        var result = await sut.Handle(
            new ImportBankStatementCommand(Guid.NewGuid(), Guid.NewGuid(),
            [
                new BankTransactionInput(DateTime.UtcNow, 1500m, "Test transfer", null, "KEY-001")
            ]),
            CancellationToken.None);

        result.Should().Be(1);
        _repo.Verify(r => r.AddRangeAsync(It.IsAny<List<BankTransaction>>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateKey_ShouldSkip()
    {
        var existing = BankTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 100m, "Existing", null, "DUPE");
        _repo.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), "DUPE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = CreateSut();
        var result = await sut.Handle(
            new ImportBankStatementCommand(Guid.NewGuid(), Guid.NewGuid(),
            [
                new BankTransactionInput(DateTime.UtcNow, 100m, "Duplicate", null, "DUPE")
            ]),
            CancellationToken.None);

        result.Should().Be(0);
    }
}

#endregion

#region RejectReconciliation

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class RejectReconciliationHandlerTests
{
    [Fact]
    public async Task Handle_MatchNotFound_ShouldThrow()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        matchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var sut = new RejectReconciliationHandler(matchRepo.Object, new Mock<IUnitOfWork>().Object);
        var act = () => sut.Handle(
            new RejectReconciliationCommand(Guid.NewGuid(), Guid.NewGuid(), "Test reason"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

#endregion

#region RunReconciliation

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class RunReconciliationHandlerTests
{
    [Fact]
    public async Task Handle_NoUnmatchedBatches_ShouldReturnZeros()
    {
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var scoringService = new Mock<IReconciliationScoringService>();
        var uow = new Mock<IUnitOfWork>();

        settlementRepo.Setup(r => r.GetUnmatchedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        bankTxRepo.Setup(r => r.GetUnreconciledAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new RunReconciliationHandler(
            settlementRepo.Object, bankTxRepo.Object, matchRepo.Object, scoringService.Object, uow.Object);

        var result = await sut.Handle(new RunReconciliationCommand(Guid.NewGuid()), CancellationToken.None);

        result.AutoMatchedCount.Should().Be(0);
        result.NeedsReviewCount.Should().Be(0);
        result.UnmatchedCount.Should().Be(0);
    }
}

#endregion
