using FluentAssertions;
using MesTech.Application.Commands.CreateExpense;
using MesTech.Application.Commands.CreateIncome;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.ImportSettlement;
using MesTech.Application.Features.Accounting.Commands.RecordCommission;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// Dalga 14 Sprint 2 — CQRS Handler coverage tests.
/// Tests null guards, happy paths, and edge cases for untested handlers.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "CqrsHandlerCoverage")]
[Trait("Phase", "Dalga14")]
public class CqrsHandlerCoverageTests
{
    // ═══════════════════════════════════════════
    // CreateIncomeHandler Tests
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CreateIncomeHandler_HappyPath_ReturnsNewId()
    {
        // Arrange
        var repo = new Mock<IIncomeRepository>();
        var uow = new Mock<IUnitOfWork>();
        Income? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Income>()))
            .Callback<Income>(i => captured = i)
            .Returns(Task.CompletedTask);

        var handler = new CreateIncomeHandler(repo.Object, uow.Object);
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var command = new CreateIncomeCommand(
            TenantId: tenantId,
            StoreId: storeId,
            Description: "Test Income",
            Amount: 1500m,
            IncomeType: IncomeType.Satis,
            InvoiceId: null,
            Date: new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            Note: "Test note"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<Income>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.Amount.Should().Be(1500m);
        captured.Description.Should().Be("Test Income");
    }

    [Fact]
    public async Task CreateIncomeHandler_NullDate_UsesUtcNow()
    {
        // Arrange
        var repo = new Mock<IIncomeRepository>();
        var uow = new Mock<IUnitOfWork>();
        Income? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Income>()))
            .Callback<Income>(i => captured = i)
            .Returns(Task.CompletedTask);

        var handler = new CreateIncomeHandler(repo.Object, uow.Object);
        var command = new CreateIncomeCommand(
            TenantId: Guid.NewGuid(),
            StoreId: null,
            Description: "Income without date",
            Amount: 500m,
            IncomeType: IncomeType.Satis,
            InvoiceId: null,
            Date: null,
            Note: null
        );

        var before = DateTime.UtcNow;

        // Act
        await handler.Handle(command, CancellationToken.None);

        var after = DateTime.UtcNow;

        // Assert
        captured.Should().NotBeNull();
        captured!.Date.Should().BeOnOrAfter(before);
        captured.Date.Should().BeOnOrBefore(after);
    }

    [Fact]
    public async Task CreateIncomeHandler_ZeroAmount_CreatesSuccessfully()
    {
        // Arrange
        var repo = new Mock<IIncomeRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.AddAsync(It.IsAny<Income>())).Returns(Task.CompletedTask);

        var handler = new CreateIncomeHandler(repo.Object, uow.Object);
        var command = new CreateIncomeCommand(
            TenantId: Guid.NewGuid(), StoreId: null,
            Description: "Zero income", Amount: 0m,
            IncomeType: IncomeType.Satis, InvoiceId: null,
            Date: DateTime.UtcNow, Note: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }

    // ═══════════════════════════════════════════
    // CreateExpenseHandler Tests
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CreateExpenseHandler_HappyPath_ReturnsNewId()
    {
        // Arrange
        var repo = new Mock<IExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        Expense? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Expense>()))
            .Callback<Expense>(e => captured = e)
            .Returns(Task.CompletedTask);

        var handler = new CreateExpenseHandler(repo.Object, uow.Object);
        var command = new CreateExpenseCommand(
            TenantId: Guid.NewGuid(),
            StoreId: Guid.NewGuid(),
            Description: "Office supplies",
            Amount: 250m,
            ExpenseType: ExpenseType.Kargo,
            Date: new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            Note: "Monthly office supplies",
            IsRecurring: true,
            RecurrencePeriod: "Monthly"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        captured.Should().NotBeNull();
        captured!.Description.Should().Be("Office supplies");
        captured.IsRecurring.Should().BeTrue();
        captured.RecurrencePeriod.Should().Be("Monthly");
    }

    [Fact]
    public async Task CreateExpenseHandler_NullDate_UsesUtcNow()
    {
        // Arrange
        var repo = new Mock<IExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        Expense? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Expense>()))
            .Callback<Expense>(e => captured = e)
            .Returns(Task.CompletedTask);

        var handler = new CreateExpenseHandler(repo.Object, uow.Object);
        var command = new CreateExpenseCommand(
            TenantId: Guid.NewGuid(), StoreId: null,
            Description: "Expense no date", Amount: 100m,
            ExpenseType: ExpenseType.Kargo, Date: null, Note: null
        );

        var before = DateTime.UtcNow;

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        captured!.Date.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task CreateExpenseHandler_NonRecurring_HasNullPeriod()
    {
        // Arrange
        var repo = new Mock<IExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        Expense? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Expense>()))
            .Callback<Expense>(e => captured = e)
            .Returns(Task.CompletedTask);

        var handler = new CreateExpenseHandler(repo.Object, uow.Object);
        var command = new CreateExpenseCommand(
            TenantId: Guid.NewGuid(), StoreId: null,
            Description: "One time expense", Amount: 50m,
            ExpenseType: ExpenseType.Kargo, Date: DateTime.UtcNow, Note: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        captured!.IsRecurring.Should().BeFalse();
        captured.RecurrencePeriod.Should().BeNull();
    }

    // ═══════════════════════════════════════════
    // CreateJournalEntryHandler Tests
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CreateJournalEntryHandler_BalancedLines_CreatesSuccessfully()
    {
        // Arrange
        var repo = new Mock<IJournalEntryRepository>();
        var uow = new Mock<IUnitOfWork>();
        JournalEntry? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        var handler = new CreateJournalEntryHandler(repo.Object, uow.Object);
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        var command = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            Description: "Test journal entry",
            ReferenceNumber: "JE-001",
            Lines: new List<JournalLineInput>
            {
                new(accountA, 1000m, 0m, "Debit line"),
                new(accountB, 0m, 1000m, "Credit line")
            }
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        captured.Should().NotBeNull();
        captured!.Lines.Should().HaveCount(2);
        captured.Description.Should().Be("Test journal entry");
    }

    [Fact]
    public async Task CreateJournalEntryHandler_ImbalancedLines_ThrowsException()
    {
        // Arrange
        var repo = new Mock<IJournalEntryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var handler = new CreateJournalEntryHandler(repo.Object, uow.Object);

        var command = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: DateTime.UtcNow,
            Description: "Imbalanced entry",
            ReferenceNumber: null,
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 1000m, 0m, "Debit"),
                new(Guid.NewGuid(), 0m, 500m, "Credit")
            }
        );

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<JournalEntryImbalanceException>();
    }

    [Fact]
    public async Task CreateJournalEntryHandler_SingleLine_ThrowsException()
    {
        // Arrange
        var repo = new Mock<IJournalEntryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var handler = new CreateJournalEntryHandler(repo.Object, uow.Object);

        var command = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: DateTime.UtcNow,
            Description: "Single line",
            ReferenceNumber: null,
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 1000m, 0m, "Single debit line")
            }
        );

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert — single unbalanced line triggers imbalance check before line count check
        await act.Should().ThrowAsync<JournalEntryImbalanceException>();
    }

    // ═══════════════════════════════════════════
    // ImportSettlementHandler Tests
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ImportSettlementHandler_HappyPath_CreatesBatchWithLines()
    {
        // Arrange
        var repo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        SettlementBatch? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<SettlementBatch>(), It.IsAny<CancellationToken>()))
            .Callback<SettlementBatch, CancellationToken>((b, _) => captured = b)
            .Returns(Task.CompletedTask);

        var handler = new ImportSettlementHandler(repo.Object, uow.Object);
        var command = new ImportSettlementCommand(
            TenantId: Guid.NewGuid(),
            Platform: "Trendyol",
            PeriodStart: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd: new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            TotalGross: 10000m,
            TotalCommission: 1500m,
            TotalNet: 8500m,
            Lines: new List<SettlementLineInput>
            {
                new("ORD-001", 5000m, 750m, 50m, 30m, 0m, 4170m),
                new("ORD-002", 5000m, 750m, 50m, 30m, 0m, 4170m)
            }
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        captured.Should().NotBeNull();
        captured!.Platform.Should().Be("Trendyol");
        captured.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportSettlementHandler_EmptyLines_CreatesBatchWithNoLines()
    {
        // Arrange
        var repo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        SettlementBatch? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<SettlementBatch>(), It.IsAny<CancellationToken>()))
            .Callback<SettlementBatch, CancellationToken>((b, _) => captured = b)
            .Returns(Task.CompletedTask);

        var handler = new ImportSettlementHandler(repo.Object, uow.Object);
        var command = new ImportSettlementCommand(
            TenantId: Guid.NewGuid(),
            Platform: "Hepsiburada",
            PeriodStart: DateTime.UtcNow.AddDays(-14),
            PeriodEnd: DateTime.UtcNow,
            TotalGross: 0m, TotalCommission: 0m, TotalNet: 0m,
            Lines: new List<SettlementLineInput>()
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        captured!.Lines.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════
    // RecordCommissionHandler Tests
    // ═══════════════════════════════════════════

    [Fact]
    public async Task RecordCommissionHandler_HappyPath_CreatesRecord()
    {
        // Arrange
        var repo = new Mock<ICommissionRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.AddAsync(It.IsAny<CommissionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RecordCommissionHandler(repo.Object, uow.Object);
        var command = new RecordCommissionCommand(
            TenantId: Guid.NewGuid(),
            Platform: "Trendyol",
            GrossAmount: 1000m,
            CommissionRate: 0.15m,
            CommissionAmount: 150m,
            ServiceFee: 5m,
            OrderId: "ORD-123",
            Category: "Electronics"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<CommissionRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordCommissionHandler_NullOptionalFields_CreatesSuccessfully()
    {
        // Arrange
        var repo = new Mock<ICommissionRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.AddAsync(It.IsAny<CommissionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RecordCommissionHandler(repo.Object, uow.Object);
        var command = new RecordCommissionCommand(
            TenantId: Guid.NewGuid(),
            Platform: "N11",
            GrossAmount: 500m,
            CommissionRate: 0.12m,
            CommissionAmount: 60m,
            ServiceFee: 0m
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }

    // ═══════════════════════════════════════════
    // CreateJournalEntryHandler — Additional Edge Cases
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CreateJournalEntryHandler_MultipleLines_AllBalanced()
    {
        // Arrange
        var repo = new Mock<IJournalEntryRepository>();
        var uow = new Mock<IUnitOfWork>();
        JournalEntry? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        var handler = new CreateJournalEntryHandler(repo.Object, uow.Object);
        var command = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: DateTime.UtcNow,
            Description: "Multi-line balanced",
            ReferenceNumber: "JE-002",
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 500m, 0m, "Debit 1"),
                new(Guid.NewGuid(), 500m, 0m, "Debit 2"),
                new(Guid.NewGuid(), 0m, 700m, "Credit 1"),
                new(Guid.NewGuid(), 0m, 300m, "Credit 2")
            }
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        captured!.Lines.Should().HaveCount(4);
    }

    [Fact]
    public async Task CreateJournalEntryHandler_WithReferenceNumber_PersistsIt()
    {
        // Arrange
        var repo = new Mock<IJournalEntryRepository>();
        var uow = new Mock<IUnitOfWork>();
        JournalEntry? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        var handler = new CreateJournalEntryHandler(repo.Object, uow.Object);
        var command = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: DateTime.UtcNow,
            Description: "Ref test",
            ReferenceNumber: "REF-2026-001",
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, null),
                new(Guid.NewGuid(), 0m, 100m, null)
            }
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        captured!.ReferenceNumber.Should().Be("REF-2026-001");
    }

    // ═══════════════════════════════════════════
    // ImportSettlementHandler — Platform validation
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ImportSettlementHandler_WhitespacePlatform_ThrowsException()
    {
        // Arrange
        var repo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        var handler = new ImportSettlementHandler(repo.Object, uow.Object);

        var command = new ImportSettlementCommand(
            TenantId: Guid.NewGuid(),
            Platform: "   ",
            PeriodStart: DateTime.UtcNow.AddDays(-7),
            PeriodEnd: DateTime.UtcNow,
            TotalGross: 1000m, TotalCommission: 100m, TotalNet: 900m,
            Lines: new List<SettlementLineInput>()
        );

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert — SettlementBatch.Create validates platform
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ═══════════════════════════════════════════
    // RecordCommissionHandler — Validation delegation
    // ═══════════════════════════════════════════

    [Fact]
    public async Task RecordCommissionHandler_EmptyPlatform_ThrowsException()
    {
        // Arrange
        var repo = new Mock<ICommissionRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var handler = new RecordCommissionHandler(repo.Object, uow.Object);

        var command = new RecordCommissionCommand(
            TenantId: Guid.NewGuid(),
            Platform: "",
            GrossAmount: 100m,
            CommissionRate: 0.10m,
            CommissionAmount: 10m,
            ServiceFee: 0m
        );

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RecordCommissionHandler_NegativeGross_ThrowsException()
    {
        // Arrange
        var repo = new Mock<ICommissionRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var handler = new RecordCommissionHandler(repo.Object, uow.Object);

        var command = new RecordCommissionCommand(
            TenantId: Guid.NewGuid(),
            Platform: "Trendyol",
            GrossAmount: -100m,
            CommissionRate: 0.15m,
            CommissionAmount: 0m,
            ServiceFee: 0m
        );

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task RecordCommissionHandler_NegativeRate_ThrowsException()
    {
        // Arrange
        var repo = new Mock<ICommissionRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var handler = new RecordCommissionHandler(repo.Object, uow.Object);

        var command = new RecordCommissionCommand(
            TenantId: Guid.NewGuid(),
            Platform: "N11",
            GrossAmount: 100m,
            CommissionRate: -0.10m,
            CommissionAmount: 0m,
            ServiceFee: 0m
        );

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ═══════════════════════════════════════════
    // CreateIncomeHandler — Verify field mapping
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CreateIncomeHandler_AllFieldsMapped()
    {
        // Arrange
        var repo = new Mock<IIncomeRepository>();
        var uow = new Mock<IUnitOfWork>();
        Income? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Income>()))
            .Callback<Income>(i => captured = i)
            .Returns(Task.CompletedTask);

        var handler = new CreateIncomeHandler(repo.Object, uow.Object);
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var date = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        var command = new CreateIncomeCommand(
            TenantId: tenantId,
            StoreId: storeId,
            Description: "Mapped income",
            Amount: 2500.50m,
            IncomeType: IncomeType.Satis,
            InvoiceId: invoiceId,
            Date: date,
            Note: "Full mapping test"
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        captured!.TenantId.Should().Be(tenantId);
        captured.StoreId.Should().Be(storeId);
        captured.Description.Should().Be("Mapped income");
        captured.Amount.Should().Be(2500.50m);
        captured.InvoiceId.Should().Be(invoiceId);
        captured.Date.Should().Be(date);
        captured.Note.Should().Be("Full mapping test");
    }

    // ═══════════════════════════════════════════
    // CreateExpenseHandler — Verify field mapping
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CreateExpenseHandler_AllFieldsMapped()
    {
        // Arrange
        var repo = new Mock<IExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        Expense? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Expense>()))
            .Callback<Expense>(e => captured = e)
            .Returns(Task.CompletedTask);

        var handler = new CreateExpenseHandler(repo.Object, uow.Object);
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var command = new CreateExpenseCommand(
            TenantId: tenantId,
            StoreId: storeId,
            Description: "Full expense",
            Amount: 3500.75m,
            ExpenseType: ExpenseType.Kargo,
            Date: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            Note: "Full mapping test",
            IsRecurring: true,
            RecurrencePeriod: "Weekly"
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        captured!.TenantId.Should().Be(tenantId);
        captured.StoreId.Should().Be(storeId);
        captured.Amount.Should().Be(3500.75m);
        captured.Note.Should().Be("Full mapping test");
        captured.IsRecurring.Should().BeTrue();
        captured.RecurrencePeriod.Should().Be("Weekly");
    }
}
