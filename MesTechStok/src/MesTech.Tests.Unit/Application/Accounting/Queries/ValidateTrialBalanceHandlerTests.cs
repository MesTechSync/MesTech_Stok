using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.ValidateTrialBalance;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Queries;

/// <summary>
/// ValidateTrialBalanceHandler tests — trial balance validation (sum debit == sum credit).
/// Verifies balanced, imbalanced, and empty scenarios using the domain validation service.
/// </summary>
[Trait("Category", "Unit")]
public class ValidateTrialBalanceHandlerTests
{
    private readonly Mock<IJournalEntryRepository> _journalRepoMock;
    private readonly TrialBalanceValidationService _validationService;
    private readonly ValidateTrialBalanceHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ValidateTrialBalanceHandlerTests()
    {
        _journalRepoMock = new Mock<IJournalEntryRepository>();
        _validationService = new TrialBalanceValidationService();

        _sut = new ValidateTrialBalanceHandler(
            _journalRepoMock.Object,
            _validationService);
    }

    private JournalEntry CreatePostedEntry(
        decimal debit, decimal credit, string description = "Test", DateTime? date = null)
    {
        var entry = JournalEntry.Create(_tenantId, date ?? DateTime.UtcNow, description);
        entry.AddLine(Guid.NewGuid(), debit, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, credit);
        entry.Post();
        return entry;
    }

    [Fact]
    public async Task Handle_BalancedEntries_ReturnsIsBalancedTrue()
    {
        // Arrange
        var entries = new List<JournalEntry>
        {
            CreatePostedEntry(5000m, 5000m, "Satis faturasi"),
            CreatePostedEntry(3000m, 3000m, "Alis faturasi")
        };

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var query = new ValidateTrialBalanceQuery(
            _tenantId,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsBalanced.Should().BeTrue();
        result.TotalDebits.Should().Be(8000m);
        result.TotalCredits.Should().Be(8000m);
        result.Difference.Should().Be(0m);
        result.JournalEntryCount.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyEntries_ReturnsBalancedWithZeros()
    {
        // Arrange
        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var query = new ValidateTrialBalanceQuery(
            _tenantId,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsBalanced.Should().BeTrue();
        result.TotalDebits.Should().Be(0m);
        result.TotalCredits.Should().Be(0m);
        result.JournalEntryCount.Should().Be(0);
    }

    [Fact]
    public void Domain_PreventsImbalancedEntry_ThrowsOnPost()
    {
        // Domain correctly prevents imbalanced entries — this verifies the guard.
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Imbalanced");
        entry.AddLine(Guid.NewGuid(), 5000m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 4000m);

        var act = () => entry.Post();

        act.Should().Throw<MesTech.Domain.Accounting.Entities.JournalEntryImbalanceException>();
    }

    [Fact]
    public async Task Handle_UnpostedEntriesExcluded_OnlyPostedCounted()
    {
        // Arrange — mix of posted and unposted entries
        var postedEntry = CreatePostedEntry(3000m, 3000m);

        var unpostedEntry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Draft entry");
        unpostedEntry.AddLine(Guid.NewGuid(), 9999m, 0m);
        unpostedEntry.AddLine(Guid.NewGuid(), 0m, 9999m);
        // NOT posted

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { postedEntry, unpostedEntry });

        var query = new ValidateTrialBalanceQuery(
            _tenantId,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — only the posted entry counts
        result.JournalEntryCount.Should().Be(1);
        result.TotalDebits.Should().Be(3000m);
        result.TotalCredits.Should().Be(3000m);
        result.IsBalanced.Should().BeTrue();
    }
}
