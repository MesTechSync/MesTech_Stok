using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;
using MesTech.Application.Features.Accounting.Commands.RejectReconciliation;
using MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class AccountingCommandHandlerGapTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly CancellationToken Ct = CancellationToken.None;

    #region UpdatePlatformCommissionRateHandler

    private readonly Mock<IPlatformCommissionRepository> _commissionRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    private UpdatePlatformCommissionRateHandler CreateUpdateHandler()
        => new(_commissionRepoMock.Object, _uowMock.Object);

    [Fact]
    public async Task UpdateCommission_NotFound_ReturnsFalse()
    {
        // Arrange
        _commissionRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), Ct))
            .ReturnsAsync((PlatformCommission?)null);

        var sut = CreateUpdateHandler();
        var command = new UpdatePlatformCommissionRateCommand(Guid.NewGuid(), Rate: 0.20m);

        // Act
        var result = await sut.Handle(command, Ct);

        // Assert
        result.Should().BeFalse();
        _commissionRepoMock.Verify(r => r.UpdateAsync(It.IsAny<PlatformCommission>(), Ct), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(Ct), Times.Never);
    }

    [Fact]
    public async Task UpdateCommission_Found_UpdatesAndReturnsTrue()
    {
        // Arrange
        var existing = new PlatformCommission
        {
            TenantId = TenantId,
            Platform = PlatformType.Trendyol,
            Rate = 0.10m,
            Type = CommissionType.Percentage,
            CategoryName = "Elektronik",
            IsActive = true
        };

        _commissionRepoMock
            .Setup(r => r.GetByIdAsync(existing.Id, Ct))
            .ReturnsAsync(existing);

        var sut = CreateUpdateHandler();
        var command = new UpdatePlatformCommissionRateCommand(existing.Id, Rate: 0.25m);

        // Act
        var result = await sut.Handle(command, Ct);

        // Assert
        result.Should().BeTrue();
        existing.Rate.Should().Be(0.25m);
        _commissionRepoMock.Verify(r => r.UpdateAsync(existing, Ct), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task UpdateCommission_PartialUpdate_OnlyChangesProvidedFields()
    {
        // Arrange
        var existing = new PlatformCommission
        {
            TenantId = TenantId,
            Platform = PlatformType.Hepsiburada,
            Rate = 0.15m,
            Type = CommissionType.Percentage,
            CategoryName = "Giyim",
            Currency = "TRY",
            IsActive = true
        };

        _commissionRepoMock
            .Setup(r => r.GetByIdAsync(existing.Id, Ct))
            .ReturnsAsync(existing);

        var sut = CreateUpdateHandler();
        // Only update CategoryName and IsActive — leave Rate, Type, Currency unchanged
        var command = new UpdatePlatformCommissionRateCommand(
            existing.Id, CategoryName: "Ayakkabi", IsActive: false);

        // Act
        var result = await sut.Handle(command, Ct);

        // Assert
        result.Should().BeTrue();
        existing.CategoryName.Should().Be("Ayakkabi");
        existing.IsActive.Should().BeFalse();
        // Unchanged fields
        existing.Rate.Should().Be(0.15m);
        existing.Type.Should().Be(CommissionType.Percentage);
        existing.Currency.Should().Be("TRY");
    }

    [Fact]
    public async Task UpdateCommission_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateUpdateHandler();
        var act = () => sut.Handle(null!, Ct);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region ApproveReconciliationHandler

    [Fact]
    public async Task ApproveReconciliation_NotFound_ThrowsInvalidOperation()
    {
        // Arrange
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();

        matchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), Ct))
            .ReturnsAsync((ReconciliationMatch?)null);

        var sut = new ApproveReconciliationHandler(
            matchRepo.Object, bankTxRepo.Object, settlementRepo.Object, uow.Object);

        var command = new ApproveReconciliationCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        var act = () => sut.Handle(command, Ct);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ApproveReconciliation_MatchOnly_ApprovesWithoutCascade()
    {
        // Arrange — match with no bank tx and no settlement
        var match = ReconciliationMatch.Create(
            TenantId, DateTime.UtcNow, 0.95m, ReconciliationStatus.AutoMatched);

        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();

        matchRepo.Setup(r => r.GetByIdAsync(match.Id, Ct)).ReturnsAsync(match);

        var sut = new ApproveReconciliationHandler(
            matchRepo.Object, bankTxRepo.Object, settlementRepo.Object, uow.Object);

        var reviewerId = Guid.NewGuid();
        var command = new ApproveReconciliationCommand(match.Id, reviewerId);

        // Act
        var result = await sut.Handle(command, Ct);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        matchRepo.Verify(r => r.UpdateAsync(match, Ct), Times.Once);
        bankTxRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), Ct), Times.Never);
        settlementRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), Ct), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task ApproveReconciliation_WithBankTx_MarksBankTxReconciled()
    {
        // Arrange
        var bankTxId = Guid.NewGuid();
        var match = ReconciliationMatch.Create(
            TenantId, DateTime.UtcNow, 0.90m, ReconciliationStatus.NeedsReview,
            bankTransactionId: bankTxId);

        var bankTx = BankTransaction.Create(
            TenantId, Guid.NewGuid(), DateTime.UtcNow, 500m, "Test deposit");

        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();

        matchRepo.Setup(r => r.GetByIdAsync(match.Id, Ct)).ReturnsAsync(match);
        bankTxRepo.Setup(r => r.GetByIdAsync(bankTxId, Ct)).ReturnsAsync(bankTx);

        var sut = new ApproveReconciliationHandler(
            matchRepo.Object, bankTxRepo.Object, settlementRepo.Object, uow.Object);

        var command = new ApproveReconciliationCommand(match.Id, Guid.NewGuid());

        // Act
        await sut.Handle(command, Ct);

        // Assert
        bankTx.IsReconciled.Should().BeTrue();
        bankTxRepo.Verify(r => r.UpdateAsync(bankTx, Ct), Times.Once);
    }

    [Fact]
    public async Task ApproveReconciliation_WithSettlementBatch_MarksSettlementReconciled()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var match = ReconciliationMatch.Create(
            TenantId, DateTime.UtcNow, 0.85m, ReconciliationStatus.AutoMatched,
            settlementBatchId: batchId);

        var batch = SettlementBatch.Create(
            TenantId, "Trendyol", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
            10000m, 1500m, 8500m);

        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();

        matchRepo.Setup(r => r.GetByIdAsync(match.Id, Ct)).ReturnsAsync(match);
        settlementRepo.Setup(r => r.GetByIdAsync(batchId, Ct)).ReturnsAsync(batch);

        var sut = new ApproveReconciliationHandler(
            matchRepo.Object, bankTxRepo.Object, settlementRepo.Object, uow.Object);

        var command = new ApproveReconciliationCommand(match.Id, Guid.NewGuid());

        // Act
        await sut.Handle(command, Ct);

        // Assert
        batch.Status.Should().Be(SettlementStatus.Reconciled);
        settlementRepo.Verify(r => r.UpdateAsync(batch, Ct), Times.Once);
    }

    [Fact]
    public async Task ApproveReconciliation_WithBothBankTxAndSettlement_CascadesBoth()
    {
        // Arrange
        var bankTxId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var match = ReconciliationMatch.Create(
            TenantId, DateTime.UtcNow, 0.99m, ReconciliationStatus.AutoMatched,
            settlementBatchId: batchId, bankTransactionId: bankTxId);

        var bankTx = BankTransaction.Create(
            TenantId, Guid.NewGuid(), DateTime.UtcNow, 8500m, "Settlement deposit");
        var batch = SettlementBatch.Create(
            TenantId, "Hepsiburada", DateTime.UtcNow.AddDays(-14), DateTime.UtcNow,
            20000m, 3000m, 17000m);

        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();

        matchRepo.Setup(r => r.GetByIdAsync(match.Id, Ct)).ReturnsAsync(match);
        bankTxRepo.Setup(r => r.GetByIdAsync(bankTxId, Ct)).ReturnsAsync(bankTx);
        settlementRepo.Setup(r => r.GetByIdAsync(batchId, Ct)).ReturnsAsync(batch);

        var sut = new ApproveReconciliationHandler(
            matchRepo.Object, bankTxRepo.Object, settlementRepo.Object, uow.Object);

        var command = new ApproveReconciliationCommand(match.Id, Guid.NewGuid());

        // Act
        await sut.Handle(command, Ct);

        // Assert
        bankTx.IsReconciled.Should().BeTrue();
        batch.Status.Should().Be(SettlementStatus.Reconciled);
        bankTxRepo.Verify(r => r.UpdateAsync(bankTx, Ct), Times.Once);
        settlementRepo.Verify(r => r.UpdateAsync(batch, Ct), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    #endregion

    #region RejectReconciliationHandler

    [Fact]
    public async Task RejectReconciliation_NotFound_ThrowsInvalidOperation()
    {
        // Arrange
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var uow = new Mock<IUnitOfWork>();

        matchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), Ct))
            .ReturnsAsync((ReconciliationMatch?)null);

        var sut = new RejectReconciliationHandler(matchRepo.Object, uow.Object);
        var command = new RejectReconciliationCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        var act = () => sut.Handle(command, Ct);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task RejectReconciliation_Found_RejectsAndSaves()
    {
        // Arrange
        var match = ReconciliationMatch.Create(
            TenantId, DateTime.UtcNow, 0.60m, ReconciliationStatus.NeedsReview);

        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var uow = new Mock<IUnitOfWork>();

        matchRepo.Setup(r => r.GetByIdAsync(match.Id, Ct)).ReturnsAsync(match);

        var sut = new RejectReconciliationHandler(matchRepo.Object, uow.Object);
        var reviewerId = Guid.NewGuid();
        var command = new RejectReconciliationCommand(match.Id, reviewerId);

        // Act
        var result = await sut.Handle(command, Ct);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        matchRepo.Verify(r => r.UpdateAsync(match, Ct), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task RejectReconciliation_WithReason_RejectsSuccessfully()
    {
        // Arrange
        var match = ReconciliationMatch.Create(
            TenantId, DateTime.UtcNow, 0.40m, ReconciliationStatus.AutoMatched);

        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var uow = new Mock<IUnitOfWork>();

        matchRepo.Setup(r => r.GetByIdAsync(match.Id, Ct)).ReturnsAsync(match);

        var sut = new RejectReconciliationHandler(matchRepo.Object, uow.Object);
        var command = new RejectReconciliationCommand(
            match.Id, Guid.NewGuid(), Reason: "Amount mismatch — bank shows 8400 TRY vs settlement 8500 TRY");

        // Act
        var result = await sut.Handle(command, Ct);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        matchRepo.Verify(r => r.UpdateAsync(match, Ct), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    #endregion
}
