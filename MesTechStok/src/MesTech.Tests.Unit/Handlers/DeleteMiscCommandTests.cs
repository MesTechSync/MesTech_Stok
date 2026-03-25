using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;
using MesTech.Application.Features.Accounting.Commands.DeactivateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.DeleteFixedExpense;
using MesTech.Application.Features.Finance.Commands.ApproveExpense;
using MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;
using MesTech.Application.Features.Stores.Commands.SaveStoreCredential;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Entities.Reporting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for DeleteFixedExpense, DeleteSavedReport, SaveStoreCredential,
/// DeactivateFixedAsset, ApproveExpense, ApproveReconciliation handlers.
/// </summary>
[Trait("Category", "Unit")]
public class DeleteMiscCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _id = Guid.NewGuid();

    // ═══════ DeleteFixedExpenseHandler ═══════

    [Fact]
    public async Task DeleteFixedExpense_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FixedExpense?)null);

        var sut = new DeleteFixedExpenseHandler(repo.Object, uow.Object);
        var cmd = new DeleteFixedExpenseCommand(_id);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteFixedExpense_Found_SoftDeletesAndSaves()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var expense = FixedExpense.Create(_tenantId, "Rent", 1000m, 15, DateTime.UtcNow);
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var sut = new DeleteFixedExpenseHandler(repo.Object, uow.Object);
        var cmd = new DeleteFixedExpenseCommand(_id);

        await sut.Handle(cmd, CancellationToken.None);

        expense.IsDeleted.Should().BeTrue();
        expense.DeletedAt.Should().NotBeNull();
        repo.Verify(r => r.UpdateAsync(expense, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ DeleteSavedReportHandler ═══════

    [Fact]
    public async Task DeleteSavedReport_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ISavedReportRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<DeleteSavedReportHandler>>();
        var sut = new DeleteSavedReportHandler(repo.Object, uow.Object, logger.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteSavedReport_NotFound_ReturnsFalse()
    {
        var repo = new Mock<ISavedReportRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<DeleteSavedReportHandler>>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SavedReport?)null);

        var sut = new DeleteSavedReportHandler(repo.Object, uow.Object, logger.Object);
        var cmd = new DeleteSavedReportCommand(_tenantId, _id);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSavedReport_WrongTenant_ReturnsFalse()
    {
        var repo = new Mock<ISavedReportRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<DeleteSavedReportHandler>>();
        var report = SavedReport.Create(Guid.NewGuid(), "Report", "Sales", "{}", Guid.NewGuid()); // different tenant
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var sut = new DeleteSavedReportHandler(repo.Object, uow.Object, logger.Object);
        var cmd = new DeleteSavedReportCommand(_tenantId, _id);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSavedReport_ValidRequest_ReturnsTrue()
    {
        var repo = new Mock<ISavedReportRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<DeleteSavedReportHandler>>();
        var report = SavedReport.Create(_tenantId, "Report", "Sales", "{}", Guid.NewGuid());
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var sut = new DeleteSavedReportHandler(repo.Object, uow.Object, logger.Object);
        var cmd = new DeleteSavedReportCommand(_tenantId, _id);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        repo.Verify(r => r.DeleteAsync(report, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ SaveStoreCredentialHandler ═══════

    [Fact]
    public async Task SaveStoreCredential_NullRequest_ThrowsArgumentNullException()
    {
        var storeRepo = new Mock<IStoreRepository>();
        var credRepo = new Mock<IStoreCredentialRepository>();
        var uow = new Mock<IUnitOfWork>();
        var encryption = new Mock<ICredentialEncryptionService>();
        var logger = new Mock<ILogger<SaveStoreCredentialHandler>>();
        var sut = new SaveStoreCredentialHandler(storeRepo.Object, credRepo.Object, uow.Object, encryption.Object, logger.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SaveStoreCredential_StoreNotFound_ThrowsInvalidOperationException()
    {
        var storeRepo = new Mock<IStoreRepository>();
        var credRepo = new Mock<IStoreCredentialRepository>();
        var uow = new Mock<IUnitOfWork>();
        var encryption = new Mock<ICredentialEncryptionService>();
        var logger = new Mock<ILogger<SaveStoreCredentialHandler>>();
        storeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Store?)null);

        var sut = new SaveStoreCredentialHandler(storeRepo.Object, credRepo.Object, uow.Object, encryption.Object, logger.Object);
        var cmd = new SaveStoreCredentialCommand
        {
            StoreId = _id,
            TenantId = _tenantId,
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string> { { "ApiKey", "xxx" } }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task SaveStoreCredential_WrongTenant_ThrowsUnauthorizedAccessException()
    {
        var storeRepo = new Mock<IStoreRepository>();
        var credRepo = new Mock<IStoreCredentialRepository>();
        var uow = new Mock<IUnitOfWork>();
        var encryption = new Mock<ICredentialEncryptionService>();
        var logger = new Mock<ILogger<SaveStoreCredentialHandler>>();
        var store = new Store { TenantId = Guid.NewGuid() }; // different tenant
        storeRepo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(store);

        var sut = new SaveStoreCredentialHandler(storeRepo.Object, credRepo.Object, uow.Object, encryption.Object, logger.Object);
        var cmd = new SaveStoreCredentialCommand
        {
            StoreId = _id,
            TenantId = _tenantId,
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string> { { "ApiKey", "xxx" } }
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task SaveStoreCredential_ValidRequest_EncryptsAndSaves()
    {
        var storeRepo = new Mock<IStoreRepository>();
        var credRepo = new Mock<IStoreCredentialRepository>();
        var uow = new Mock<IUnitOfWork>();
        var encryption = new Mock<ICredentialEncryptionService>();
        var logger = new Mock<ILogger<SaveStoreCredentialHandler>>();
        var store = new Store { TenantId = _tenantId };
        storeRepo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(store);
        credRepo.Setup(r => r.GetByStoreIdAsync(_id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>());
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted_value");

        var sut = new SaveStoreCredentialHandler(storeRepo.Object, credRepo.Object, uow.Object, encryption.Object, logger.Object);
        var cmd = new SaveStoreCredentialCommand
        {
            StoreId = _id,
            TenantId = _tenantId,
            Platform = "Trendyol",
            CredentialType = "api_key",
            Fields = new Dictionary<string, string> { { "ApiKey", "xxx" }, { "Secret", "yyy" } }
        };

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        encryption.Verify(e => e.Encrypt(It.IsAny<string>()), Times.Exactly(2));
        credRepo.Verify(r => r.AddAsync(It.IsAny<StoreCredential>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ DeactivateFixedAssetHandler ═══════

    [Fact]
    public async Task DeactivateFixedAsset_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.FixedAsset?)null);

        var sut = new DeactivateFixedAssetHandler(repo.Object, uow.Object);
        var cmd = new DeactivateFixedAssetCommand(_id, _tenantId);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task DeactivateFixedAsset_ValidRequest_ReturnsUnitValue()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var uow = new Mock<IUnitOfWork>();
        var asset = MesTech.Domain.Accounting.Entities.FixedAsset.Create(
            _tenantId, "CNC", "253", 10000m, DateTime.UtcNow.AddYears(-1), 5,
            DepreciationMethod.StraightLine);
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        var sut = new DeactivateFixedAssetHandler(repo.Object, uow.Object);
        var cmd = new DeactivateFixedAssetCommand(_id, _tenantId);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        asset.IsActive.Should().BeFalse();
        repo.Verify(r => r.UpdateAsync(asset, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ ApproveExpenseHandler ═══════

    [Fact]
    public async Task ApproveExpense_NotFound_ThrowsInvalidOperationException()
    {
        var repo = new Mock<IFinanceExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinanceExpense?)null);

        var sut = new ApproveExpenseHandler(repo.Object, uow.Object);
        var cmd = new ApproveExpenseCommand(_id, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task ApproveExpense_ValidRequest_ReturnsUnitValue()
    {
        var repo = new Mock<IFinanceExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var expense = FinanceExpense.Create(
            _tenantId, "Office", 100m, MesTech.Domain.Enums.ExpenseCategory.Software,
            DateTime.UtcNow, null, null, null);
        expense.Submit(); // Must be Submitted before Approve
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var sut = new ApproveExpenseHandler(repo.Object, uow.Object);
        var cmd = new ApproveExpenseCommand(_id, Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ ApproveReconciliationHandler ═══════

    [Fact]
    public async Task ApproveReconciliation_NotFound_ThrowsInvalidOperationException()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        matchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var sut = new ApproveReconciliationHandler(matchRepo.Object, bankTxRepo.Object, settlementRepo.Object, uow.Object);
        var cmd = new ApproveReconciliationCommand(_id, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task ApproveReconciliation_ValidMatch_ApprovesAndSaves()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var bankTxRepo = new Mock<IBankTransactionRepository>();
        var settlementRepo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.95m, ReconciliationStatus.NeedsReview);
        matchRepo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(match);

        var sut = new ApproveReconciliationHandler(matchRepo.Object, bankTxRepo.Object, settlementRepo.Object, uow.Object);
        var cmd = new ApproveReconciliationCommand(_id, Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        matchRepo.Verify(r => r.UpdateAsync(match, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
