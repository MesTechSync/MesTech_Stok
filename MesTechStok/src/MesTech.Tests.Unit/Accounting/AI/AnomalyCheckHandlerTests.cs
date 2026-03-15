using FluentAssertions;
using MassTransit;
using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Events;
using MesTech.Infrastructure.AI.Accounting;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.AI;

/// <summary>
/// AnomalyCheckHandler tests — duplicate detection and abnormal amount checks.
/// </summary>
[Trait("Category", "Unit")]
public class AnomalyCheckHandlerTests
{
    private readonly Mock<IJournalEntryRepository> _journalRepoMock;
    private readonly Mock<IPublishEndpoint> _publishMock;
    private readonly AnomalyCheckHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AnomalyCheckHandlerTests()
    {
        _journalRepoMock = new Mock<IJournalEntryRepository>();
        _publishMock = new Mock<IPublishEndpoint>();

        _sut = new AnomalyCheckHandler(
            _journalRepoMock.Object,
            _publishMock.Object,
            new Mock<ILogger<AnomalyCheckHandler>>().Object);
    }

    private DomainEventNotification<LedgerPostedEvent> CreateNotification(
        decimal amount,
        DateTime? entryDate = null)
    {
        var evt = new LedgerPostedEvent
        {
            TenantId = _tenantId,
            JournalEntryId = Guid.NewGuid(),
            EntryDate = entryDate ?? DateTime.UtcNow,
            TotalAmount = amount,
            LineCount = 2
        };

        return new DomainEventNotification<LedgerPostedEvent>(evt);
    }

    private JournalEntry CreatePostedEntry(decimal amount, DateTime entryDate)
    {
        var entry = JournalEntry.Create(_tenantId, entryDate, "Test entry");
        var accountId = Guid.NewGuid();

        entry.AddLine(accountId, amount, 0m, "Debit");
        entry.AddLine(accountId, 0m, amount, "Credit");
        entry.Post();

        return entry;
    }

    // ── Duplicate Detection ─────────────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateEntry_PublishesAnomaly()
    {
        var now = DateTime.UtcNow;
        var amount = 5000m;

        // Setup: existing entry with same amount in last 24 hours
        var existingEntry = CreatePostedEntry(amount, now.AddHours(-2));

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { existingEntry });

        var notification = CreateNotification(amount, now);

        await _sut.Handle(notification, CancellationToken.None);

        // Should publish FinanceAnomalyDetectedEvent for duplicate
        _publishMock.Verify(
            p => p.Publish(
                It.Is<FinanceAnomalyDetectedEvent>(e =>
                    e.AnomalyType == "Duplicate" &&
                    e.ActualAmount == amount),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task Handle_NormalEntry_NoDuplicate_NoPublish()
    {
        var now = DateTime.UtcNow;

        // No entries in the period
        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var notification = CreateNotification(1000m, now);

        await _sut.Handle(notification, CancellationToken.None);

        _publishMock.Verify(
            p => p.Publish(
                It.Is<FinanceAnomalyDetectedEvent>(e => e.AnomalyType == "Duplicate"),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_DifferentAmount_NoDuplicate()
    {
        var now = DateTime.UtcNow;

        // Existing entry with different amount
        var existingEntry = CreatePostedEntry(1000m, now.AddHours(-2));

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { existingEntry });

        var notification = CreateNotification(5000m, now);

        await _sut.Handle(notification, CancellationToken.None);

        _publishMock.Verify(
            p => p.Publish(
                It.Is<FinanceAnomalyDetectedEvent>(e => e.AnomalyType == "Duplicate"),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }

    // ── Abnormal Amount Detection ───────────────────────────────────

    [Fact]
    public async Task Handle_AbnormalExpense_PublishesAnomaly()
    {
        var now = DateTime.UtcNow;

        // Setup: 5 historical entries averaging 1000, new entry is 5000 (5x average > 3x threshold)
        var historicalEntries = Enumerable.Range(1, 5)
            .Select(i => CreatePostedEntry(1000m, now.AddDays(-i)))
            .ToList();

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalEntries);

        var notification = CreateNotification(5000m, now);

        await _sut.Handle(notification, CancellationToken.None);

        _publishMock.Verify(
            p => p.Publish(
                It.Is<FinanceAnomalyDetectedEvent>(e =>
                    e.AnomalyType == "AbnormalExpense"),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task Handle_NormalAmount_NoAbnormalPublish()
    {
        var now = DateTime.UtcNow;

        // Average = 1000, new = 2000 (2x, below 3x threshold)
        var historicalEntries = Enumerable.Range(1, 5)
            .Select(i => CreatePostedEntry(1000m, now.AddDays(-i)))
            .ToList();

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalEntries);

        var notification = CreateNotification(2000m, now);

        await _sut.Handle(notification, CancellationToken.None);

        _publishMock.Verify(
            p => p.Publish(
                It.Is<FinanceAnomalyDetectedEvent>(e => e.AnomalyType == "AbnormalExpense"),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_InsufficientData_SkipsAbnormalCheck()
    {
        var now = DateTime.UtcNow;

        // Less than 3 entries — skip abnormal check
        var fewEntries = new List<JournalEntry>
        {
            CreatePostedEntry(100m, now.AddDays(-1))
        };

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fewEntries);

        var notification = CreateNotification(100000m, now);

        await _sut.Handle(notification, CancellationToken.None);

        // Should not publish abnormal event (insufficient data)
        _publishMock.Verify(
            p => p.Publish(
                It.Is<FinanceAnomalyDetectedEvent>(e => e.AnomalyType == "AbnormalExpense"),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_EmptyHistory_NoAnomalies()
    {
        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        var notification = CreateNotification(1000m);

        await _sut.Handle(notification, CancellationToken.None);

        _publishMock.Verify(
            p => p.Publish(
                It.IsAny<FinanceAnomalyDetectedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_ExactlyThreeHistoricalEntries_ChecksAbnormal()
    {
        var now = DateTime.UtcNow;

        // Exactly 3 entries (minimum for check)
        var entries = Enumerable.Range(1, 3)
            .Select(i => CreatePostedEntry(100m, now.AddDays(-i)))
            .ToList();

        _journalRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                _tenantId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        // 500 > 100 * 3 = 300 => abnormal
        var notification = CreateNotification(500m, now);

        await _sut.Handle(notification, CancellationToken.None);

        _publishMock.Verify(
            p => p.Publish(
                It.Is<FinanceAnomalyDetectedEvent>(e => e.AnomalyType == "AbnormalExpense"),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }
}
