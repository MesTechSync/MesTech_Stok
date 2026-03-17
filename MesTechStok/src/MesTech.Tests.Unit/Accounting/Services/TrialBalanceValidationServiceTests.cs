using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Tests.Unit.Accounting.Services;

[Trait("Category", "Unit")]
public class TrialBalanceValidationServiceTests
{
    private readonly TrialBalanceValidationService _sut = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void ValidateFromEntries_BalancedEntries_ShouldReturnBalanced()
    {
        var entries = new List<JournalEntry>();
        var entry = CreateBalancedPostedEntry(1000m);
        entries.Add(entry);

        var result = _sut.ValidateFromEntries(entries);

        result.IsBalanced.Should().BeTrue();
        result.TotalDebits.Should().Be(1000m);
        result.TotalCredits.Should().Be(1000m);
        result.Difference.Should().Be(0m);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateFromEntries_MultipleBalancedEntries_ShouldReturnBalanced()
    {
        var entries = new List<JournalEntry>
        {
            CreateBalancedPostedEntry(1000m),
            CreateBalancedPostedEntry(2500m),
            CreateBalancedPostedEntry(500m)
        };

        var result = _sut.ValidateFromEntries(entries);

        result.IsBalanced.Should().BeTrue();
        result.TotalDebits.Should().Be(4000m);
        result.TotalCredits.Should().Be(4000m);
        result.JournalEntryCount.Should().Be(3);
    }

    [Fact]
    public void ValidateFromEntries_EmptyList_ShouldReturnBalanced()
    {
        var entries = new List<JournalEntry>();

        var result = _sut.ValidateFromEntries(entries);

        result.IsBalanced.Should().BeTrue();
        result.TotalDebits.Should().Be(0m);
        result.TotalCredits.Should().Be(0m);
        result.JournalEntryCount.Should().Be(0);
    }

    [Fact]
    public void ValidateFromEntries_SkipsUnpostedEntries()
    {
        var entries = new List<JournalEntry>();
        // Create an unposted entry
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Unposted entry");
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();
        entry.AddLine(accountId1, 1000m, 0m);
        entry.AddLine(accountId2, 0m, 1000m);
        // Do NOT post — leave it unposted
        entries.Add(entry);

        var result = _sut.ValidateFromEntries(entries);

        result.IsBalanced.Should().BeTrue();
        result.JournalEntryCount.Should().Be(0);
    }

    [Fact]
    public void ValidateFromEntries_NullInput_ShouldThrow()
    {
        var act = () => _sut.ValidateFromEntries(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateAsync_ShouldThrowInvalidOperation()
    {
        var act = async () => await _sut.ValidateAsync(
            _tenantId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        act.Should().ThrowAsync<InvalidOperationException>();
    }

    private JournalEntry CreateBalancedPostedEntry(decimal amount)
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, $"Test entry {amount}");
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();
        entry.AddLine(accountId1, amount, 0m);
        entry.AddLine(accountId2, 0m, amount);
        entry.Post();
        return entry;
    }
}
