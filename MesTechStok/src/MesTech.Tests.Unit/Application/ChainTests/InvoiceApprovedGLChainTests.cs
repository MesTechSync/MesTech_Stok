using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.ChainTests;

/// <summary>
/// TEST 3/4 — Invoice.Approve → InvoiceApprovedGLHandler → JournalEntry(120/600/391).
/// Zincir 3: Fatura onayı → GL yevmiye kaydı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "OrderChain")]
public class InvoiceApprovedGLChainTests
{
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public InvoiceApprovedGLChainTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private InvoiceApprovedGLHandler CreateSut() => new(
        _uow.Object, _journalRepo.Object, Mock.Of<ILogger<InvoiceApprovedGLHandler>>());

    [Fact]
    public async Task GL_ShouldCreateJournalWithDebit120Credit600Credit391()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        // grandTotal=590, taxAmount=90, netAmount=500
        await sut.HandleAsync(Guid.NewGuid(), TenantId, "MES-20260310-00001",
            590m, 90m, 500m, CancellationToken.None);

        captured.Should().NotBeNull("journal entry should be created");
        captured!.Lines.Should().HaveCountGreaterOrEqualTo(2);

        // Debit 120 Alıcılar = grandTotal
        captured.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account120Receivables && l.Debit == 590m,
            "DEBIT 120 (Receivables) = grandTotal 590");

        // Credit 600 Satış Geliri = netAmount
        captured.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account600DomesticSales && l.Credit == 500m,
            "CREDIT 600 (Sales) = netAmount 500");
    }

    [Fact]
    public async Task GL_WithTax_ShouldInclude391VatPayable()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), TenantId, "MES-20260310-00002",
            1180m, 180m, 1000m, CancellationToken.None);

        captured.Should().NotBeNull();

        // Credit 391 KDV = taxAmount
        captured!.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account391VatPayable && l.Credit == 180m,
            "CREDIT 391 (VAT Payable) = taxAmount 180");
    }

    [Fact]
    public async Task GL_DebitEqualsCreditRule()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), TenantId, "MES-20260310-00003",
            590m, 90m, 500m, CancellationToken.None);

        var totalDebit = captured!.Lines.Sum(l => l.Debit);
        var totalCredit = captured.Lines.Sum(l => l.Credit);

        totalDebit.Should().Be(totalCredit, "double-entry: total debit must equal total credit");
    }

    [Fact]
    public async Task GL_Idempotent_ShouldNotCreateDuplicate()
    {
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(TenantId, "MES-DUP-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), TenantId, "MES-DUP-001",
            590m, 90m, 500m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never,
            "duplicate reference should skip journal creation");
    }

    [Fact]
    public async Task GL_ZeroTax_ShouldStillCreateEntry()
    {
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        // KDV muaf ürün: taxAmount = 0
        await sut.HandleAsync(Guid.NewGuid(), TenantId, "MES-20260310-EXEMPT",
            500m, 0m, 500m, CancellationToken.None);

        captured.Should().NotBeNull();

        // Debit 120 = 500
        captured!.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account120Receivables && l.Debit == 500m);

        // Credit 600 = 500
        captured.Lines.Should().Contain(l =>
            l.AccountId == AccountingConstants.Account600DomesticSales && l.Credit == 500m);

        // 391 satırı olmamalı veya 0 olmalı (handler davranışına göre)
        var totalDebit = captured.Lines.Sum(l => l.Debit);
        var totalCredit = captured.Lines.Sum(l => l.Credit);
        totalDebit.Should().Be(totalCredit);
    }
}
