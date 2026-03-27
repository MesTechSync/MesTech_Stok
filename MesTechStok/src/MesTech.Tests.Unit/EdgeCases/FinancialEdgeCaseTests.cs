using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.EventHandlers;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Queries.GetTrialBalance;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AccountType = MesTech.Domain.Accounting.Enums.AccountType;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// Financial handler edge cases — journal balance, commission GL, trial balance empty.
/// Dalga 14+15 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class FinancialEdgeCaseTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _account1 = Guid.NewGuid();
    private readonly Guid _account2 = Guid.NewGuid();

    #region CreateJournalEntryHandler

    [Fact]
    public async Task CreateJournalEntry_ExactlyBalanced_ReturnsEntryId()
    {
        // Arrange
        var repo = new Mock<MesTech.Domain.Interfaces.IJournalEntryRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateJournalEntryHandler(repo.Object, uow.Object);
        var command = new CreateJournalEntryCommand(
            TenantId: _tenantId,
            EntryDate: DateTime.UtcNow,
            Description: "Balanced entry test",
            ReferenceNumber: "TEST-001",
            Lines: new List<JournalLineInput>
            {
                new(_account1, 500m, 0m, "Debit line"),
                new(_account2, 0m, 500m, "Credit line")
            });

        // Act
        var entryId = await handler.Handle(command, CancellationToken.None);

        // Assert
        entryId.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CreateJournalEntry_SingleLine_FailsValidation()
    {
        // A single line can never be balanced — JournalEntry.Validate() checks
        // balance first (debit != credit throws JournalEntryImbalanceException),
        // then line count (< 2 throws InvalidOperationException).
        // With 1 debit-only line: debit=100, credit=0 → imbalance detected first.
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Single line", "REF-001");
        entry.AddLine(_account1, 100m, 0m, "Only debit");

        var act = () => entry.Validate();

        act.Should().Throw<JournalEntryImbalanceException>();
    }

    [Fact]
    public void CreateJournalEntry_UnbalancedLines_ThrowsImbalanceException()
    {
        // Arrange — debit 100, credit 50 → imbalanced
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Unbalanced", "REF-002");
        entry.AddLine(_account1, 100m, 0m, "Debit");
        entry.AddLine(_account2, 0m, 50m, "Credit");

        var act = () => entry.Validate();

        act.Should().Throw<JournalEntryImbalanceException>();
    }

    [Fact]
    public void CreateJournalEntry_Validator_SingleLine_ShouldFail()
    {
        // FluentValidation check: must have at least 2 lines
        var validator = new CreateJournalEntryValidator();
        var command = new CreateJournalEntryCommand(
            TenantId: _tenantId,
            EntryDate: DateTime.UtcNow,
            Description: "Single line entry",
            ReferenceNumber: null,
            Lines: new List<JournalLineInput>
            {
                new(_account1, 100m, 0m, "Only line")
            });

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("at least 2 lines"));
    }

    [Fact]
    public void CreateJournalEntry_Validator_Unbalanced_ShouldFail()
    {
        var validator = new CreateJournalEntryValidator();
        var command = new CreateJournalEntryCommand(
            TenantId: _tenantId,
            EntryDate: DateTime.UtcNow,
            Description: "Unbalanced validator test",
            ReferenceNumber: null,
            Lines: new List<JournalLineInput>
            {
                new(_account1, 200m, 0m, "Debit"),
                new(_account2, 0m, 100m, "Credit")
            });

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("debit must equal total credit"));
    }

    #endregion

    #region CommissionChargedGLHandler

    [Fact]
    public async Task CommissionChargedGL_VerySmallAmount_CreatesGLEntry()
    {
        // Arrange — 0.01 TL commission should still create GL record
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var logger = NullLogger<CommissionChargedGLHandler>.Instance;

        var handler = new CommissionChargedGLHandler(uow.Object, logger);

        // Act
        await handler.HandleAsync(
            orderId: Guid.NewGuid(),
            tenantId: _tenantId,
            platform: PlatformType.Trendyol,
            commissionAmount: 0.01m,
            commissionRate: 0.001m,
            ct: CancellationToken.None);

        // Assert — SaveChanges called means GL entry was created
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommissionChargedGL_VeryLargeAmount_HandlesCorrectly()
    {
        // Arrange — 999,999.99 TL commission
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var logger = NullLogger<CommissionChargedGLHandler>.Instance;

        var handler = new CommissionChargedGLHandler(uow.Object, logger);

        // Act
        await handler.HandleAsync(
            orderId: Guid.NewGuid(),
            tenantId: _tenantId,
            platform: PlatformType.Hepsiburada,
            commissionAmount: 999999.99m,
            commissionRate: 0.15m,
            ct: CancellationToken.None);

        // Assert
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommissionChargedGL_ZeroAmount_SkipsGLCreation()
    {
        // Arrange — 0 commission should be skipped (guard clause)
        var uow = new Mock<IUnitOfWork>();
        var logger = NullLogger<CommissionChargedGLHandler>.Instance;

        var handler = new CommissionChargedGLHandler(uow.Object, logger);

        // Act
        await handler.HandleAsync(
            orderId: Guid.NewGuid(),
            tenantId: _tenantId,
            platform: PlatformType.Trendyol,
            commissionAmount: 0m,
            commissionRate: 0m,
            ct: CancellationToken.None);

        // Assert — SaveChanges NOT called
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CommissionChargedGL_NegativeAmount_SkipsGLCreation()
    {
        // Arrange — negative commission should be skipped (guard clause <= 0)
        var uow = new Mock<IUnitOfWork>();
        var logger = NullLogger<CommissionChargedGLHandler>.Instance;

        var handler = new CommissionChargedGLHandler(uow.Object, logger);

        // Act
        await handler.HandleAsync(
            orderId: Guid.NewGuid(),
            tenantId: _tenantId,
            platform: PlatformType.N11,
            commissionAmount: -5m,
            commissionRate: 0.10m,
            ct: CancellationToken.None);

        // Assert
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetTrialBalanceHandler

    [Fact]
    public async Task GetTrialBalance_EmptyGL_ReturnsZeroBalance()
    {
        // Arrange — no accounts, no journal entries
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        var journalRepo = new Mock<IJournalEntryRepository>();

        accountRepo.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ChartOfAccounts>());

        journalRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JournalEntry>());

        var handler = new GetTrialBalanceHandler(accountRepo.Object, journalRepo.Object);
        var query = new GetTrialBalanceQuery(_tenantId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Lines.Should().BeEmpty();
        result.GrandTotalOpeningDebit.Should().Be(0m);
        result.GrandTotalOpeningCredit.Should().Be(0m);
        result.GrandTotalPeriodDebit.Should().Be(0m);
        result.GrandTotalPeriodCredit.Should().Be(0m);
        result.GrandTotalClosingDebit.Should().Be(0m);
        result.GrandTotalClosingCredit.Should().Be(0m);
    }

    [Fact]
    public async Task GetTrialBalance_AccountsWithNoMovement_ReturnsEmptyLines()
    {
        // Arrange — accounts exist but no journal entries
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        var journalRepo = new Mock<IJournalEntryRepository>();

        var account = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        accountRepo.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { account });

        journalRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<JournalEntry>());

        var handler = new GetTrialBalanceHandler(accountRepo.Object, journalRepo.Object);
        var query = new GetTrialBalanceQuery(_tenantId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert — account exists but no movement → filtered out (all zeros)
        result.Lines.Should().BeEmpty();
    }

    #endregion
}
