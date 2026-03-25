using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// CreateJournalEntryHandler: Z3 muhasebe zinciri — yevmiye kaydı.
/// Kritik iş kuralları:
///   - Borç = Alacak dengesi (Validate() ile garanti)
///   - En az 2 satır zorunlu
///   - Her satırda AccountId zorunlu
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "AccountingChain")]
public class CreateJournalEntryHandlerTests
{
    private readonly Mock<IJournalEntryRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public CreateJournalEntryHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private CreateJournalEntryHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_BalancedEntry_ReturnsGuid()
    {
        // Arrange — dengeli yevmiye: 1000 borç = 1000 alacak
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();
        var cmd = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: DateTime.UtcNow,
            Description: "Satış hasılatı kaydı",
            ReferenceNumber: "JE-001",
            Lines: new List<JournalLineInput>
            {
                new(accountId1, Debit: 1000m, Credit: 0m, Description: "100 Kasa"),
                new(accountId2, Debit: 0m, Credit: 1000m, Description: "600 Satış Geliri")
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThreeLineBalancedEntry_ReturnsGuid()
    {
        // Arrange — 3 satırlı yevmiye: 1000 = 600 + 400
        var cmd = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: DateTime.UtcNow,
            Description: "Bölünmüş ödeme",
            ReferenceNumber: "JE-002",
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), Debit: 1000m, Credit: 0m, Description: "100 Kasa"),
                new(Guid.NewGuid(), Debit: 0m, Credit: 600m, Description: "320 Alıcılar"),
                new(Guid.NewGuid(), Debit: 0m, Credit: 400m, Description: "340 Diğer Borçlar")
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_UnbalancedEntry_ThrowsOnValidation()
    {
        // Arrange — dengesiz: 1000 borç ≠ 500 alacak
        var cmd = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: DateTime.UtcNow,
            Description: "Dengesiz kayıt",
            ReferenceNumber: "JE-BAD",
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), Debit: 1000m, Credit: 0m, Description: "Borç"),
                new(Guid.NewGuid(), Debit: 0m, Credit: 500m, Description: "Alacak — eksik!")
            });

        var handler = CreateHandler();

        // Act & Assert — domain Validate() fırlatmalı
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(cmd, CancellationToken.None));

        // Persist OLMAMALI
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var handler = CreateHandler();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CapturesCorrectLines()
    {
        // Arrange
        JournalEntry? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je)
            .Returns(Task.CompletedTask);

        var cmd = new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: new DateTime(2026, 3, 25),
            Description: "KDV tahsilatı",
            ReferenceNumber: "JE-KDV",
            Lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), Debit: 1180m, Credit: 0m, Description: "100 Kasa"),
                new(Guid.NewGuid(), Debit: 0m, Credit: 1000m, Description: "600 Satış"),
                new(Guid.NewGuid(), Debit: 0m, Credit: 180m, Description: "391 KDV")
            });

        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert — doğru sayıda satır eklendi mi?
        captured.Should().NotBeNull();
        captured!.Lines.Should().HaveCount(3);
        captured.Description.Should().Be("KDV tahsilatı");
    }
}
