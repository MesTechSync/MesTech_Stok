using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class ReconciliationMatchTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.95m,
            ReconciliationStatus.AutoMatched,
            Guid.NewGuid(), Guid.NewGuid());

        match.Should().NotBeNull();
        match.TenantId.Should().Be(_tenantId);
        match.Confidence.Should().Be(0.95m);
        match.Status.Should().Be(ReconciliationStatus.AutoMatched);
    }

    [Fact]
    public void Create_Confidence_ShouldBeBetween0And1()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.85m,
            ReconciliationStatus.AutoMatched);

        match.Confidence.Should().BeInRange(0m, 1m);
    }

    [Fact]
    public void Create_WithConfidenceAbove1_ShouldThrow()
    {
        var act = () => ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 1.5m,
            ReconciliationStatus.AutoMatched);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeConfidence_ShouldThrow()
    {
        var act = () => ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, -0.1m,
            ReconciliationStatus.AutoMatched);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithZeroConfidence_ShouldSucceed()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0m,
            ReconciliationStatus.NeedsReview);

        match.Confidence.Should().Be(0m);
    }

    [Fact]
    public void Create_WithConfidence1_ShouldSucceed()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 1m,
            ReconciliationStatus.AutoMatched);

        match.Confidence.Should().Be(1m);
    }

    [Fact]
    public void Create_AutoMatched_ShouldRaiseEvent()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.90m,
            ReconciliationStatus.AutoMatched,
            Guid.NewGuid(), Guid.NewGuid());

        match.DomainEvents.Should().ContainSingle(e => e is ReconciliationMatchedEvent);
    }

    [Fact]
    public void Create_NeedsReview_ShouldNotRaiseEvent()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.50m,
            ReconciliationStatus.NeedsReview);

        match.DomainEvents.Should().NotContain(e => e is ReconciliationMatchedEvent);
    }

    [Fact]
    public void Approve_ShouldSetStatusToManualMatch()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.70m,
            ReconciliationStatus.NeedsReview);

        match.Approve("admin@mestech.com");

        match.Status.Should().Be(ReconciliationStatus.ManualMatch);
        match.ReviewedBy.Should().Be("admin@mestech.com");
        match.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_WithEmptyReviewer_ShouldThrow()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.70m,
            ReconciliationStatus.NeedsReview);

        var act = () => match.Approve("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reject_ShouldSetStatusToRejected()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.30m,
            ReconciliationStatus.NeedsReview);

        match.Reject("admin@mestech.com");

        match.Status.Should().Be(ReconciliationStatus.Rejected);
        match.ReviewedBy.Should().Be("admin@mestech.com");
        match.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_WithEmptyReviewer_ShouldThrow()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.30m,
            ReconciliationStatus.NeedsReview);

        var act = () => match.Reject("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Approve_ShouldUpdateUpdatedAt()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.70m,
            ReconciliationStatus.NeedsReview);

        match.Approve("admin");

        match.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullSettlementBatchId_ShouldAllowNull()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.50m,
            ReconciliationStatus.NeedsReview);

        match.SettlementBatchId.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullBankTransactionId_ShouldAllowNull()
    {
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.50m,
            ReconciliationStatus.NeedsReview);

        match.BankTransactionId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetMatchDate()
    {
        var date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var match = ReconciliationMatch.Create(
            _tenantId, date, 0.90m,
            ReconciliationStatus.AutoMatched);

        match.MatchDate.Should().Be(date);
    }
}
