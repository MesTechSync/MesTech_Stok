using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// CreateJournalEntryHandler tests — balanced/unbalanced journal entry creation.
/// </summary>
[Trait("Category", "Unit")]
public class CreateJournalEntryHandlerTests
{
    private readonly Mock<IJournalEntryRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly CreateJournalEntryHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateJournalEntryHandlerTests()
    {
        _repoMock = new Mock<IJournalEntryRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _sut = new CreateJournalEntryHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_BalancedEntry_ReturnsGuidAndCallsAddAsync()
    {
        // Arrange
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();

        var command = new CreateJournalEntryCommand(
            _tenantId,
            DateTime.UtcNow,
            "Kasa giris",
            "REF-001",
            new List<JournalLineInput>
            {
                new(accountId1, 1000m, 0m, "Kasa borc"),
                new(accountId2, 0m, 1000m, "Banka alacak")
            });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(It.Is<JournalEntry>(e =>
                e.Description == "Kasa giris" && e.Lines.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_UnbalancedEntry_ThrowsJournalEntryImbalanceException()
    {
        // Arrange
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();

        var command = new CreateJournalEntryCommand(
            _tenantId,
            DateTime.UtcNow,
            "Dengesiz kayit",
            null,
            new List<JournalLineInput>
            {
                new(accountId1, 1000m, 0m, "Borc"),
                new(accountId2, 0m, 500m, "Alacak") // 1000 != 500
            });

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<JournalEntryImbalanceException>();

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_SingleLine_ThrowsOnValidation()
    {
        // Arrange — single line is both imbalanced and < 2 lines
        var accountId = Guid.NewGuid();

        var command = new CreateJournalEntryCommand(
            _tenantId,
            DateTime.UtcNow,
            "Tek satirli kayit",
            null,
            new List<JournalLineInput>
            {
                new(accountId, 1000m, 0m, "Borc")
            });

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert — imbalance check fires first (debit 1000 != credit 0)
        await act.Should().ThrowAsync<JournalEntryImbalanceException>();

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_MultipleLines_AllAdded()
    {
        // Arrange
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();
        var accountId3 = Guid.NewGuid();

        var command = new CreateJournalEntryCommand(
            _tenantId,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            "Multi-line entry",
            "REF-002",
            new List<JournalLineInput>
            {
                new(accountId1, 500m, 0m, null),
                new(accountId2, 500m, 0m, null),
                new(accountId3, 0m, 1000m, null)
            });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(It.Is<JournalEntry>(e => e.Lines.Count == 3),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
