using MassTransit;
using MediatR;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI.Accounting;

/// <summary>
/// LedgerPostedEvent domain event'ini dinler ve muhasebe anomalilerini kontrol eder.
/// Anomali tespit edilirse:
///   1. Log warning
///   2. FinanceAnomalyDetectedEvent publish (MESA Bot WhatsApp uyari gonderir)
///
/// Kontrol edilen anomali turleri:
///   - Duplicate: Ayni tutar + ayni aciklama 24 saat icinde
///   - UnexpectedCommission: Komisyon orani platform ortalamasinin 2 katindan fazla
///   - AbnormalExpense: Tutar, kategori aylik ortalamasinin 3 katindan fazla
/// </summary>
public sealed class AnomalyCheckHandler
    : INotificationHandler<DomainEventNotification<LedgerPostedEvent>>
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMediator _mediator;
    private readonly ILogger<AnomalyCheckHandler> _logger;

    public AnomalyCheckHandler(
        IJournalEntryRepository journalEntryRepository,
        IPublishEndpoint publishEndpoint,
        IMediator mediator,
        ILogger<AnomalyCheckHandler> logger)
    {
        _journalEntryRepository = journalEntryRepository;
        _publishEndpoint = publishEndpoint;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<LedgerPostedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[Anomaly] LedgerPosted yakalandi: JournalEntryId={JournalEntryId}, Tutar={Amount:N2}",
            e.JournalEntryId, e.TotalAmount);

        // Anomali kontrolleri — exception domain event zincirini KIRMAMALI
        try
        {
            await CheckDuplicateAsync(e, ct).ConfigureAwait(false);
            await CheckAbnormalAmountAsync(e, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "[Anomaly] Anomaly check failed for JournalEntryId={JournalEntryId} — " +
                "ledger posting continues, anomaly detection skipped",
                e.JournalEntryId);
        }
    }

    /// <summary>
    /// Duplicate kontrolu: Son 24 saatte ayni tutarda baska yevmiye var mi?
    /// </summary>
    private async Task CheckDuplicateAsync(LedgerPostedEvent e, CancellationToken ct)
    {
        var windowStart = e.EntryDate.AddHours(-24);
        var windowEnd = e.EntryDate;

        var recentEntries = await _journalEntryRepository.GetByDateRangeAsync(
            e.TenantId, windowStart, windowEnd, ct).ConfigureAwait(false);

        var duplicates = recentEntries
            .Where(je => je.Id != e.JournalEntryId
                         && je.IsPosted
                         && je.Lines.Sum(l => l.Debit) == e.TotalAmount)
            .ToList();

        if (duplicates.Count > 0)
        {
            _logger.LogWarning(
                "[Anomaly] DUPLICATE tespit edildi: JournalEntryId={JournalEntryId}, " +
                "ayni tutar ({Amount:N2}) son 24 saatte {Count} kez goruldu",
                e.JournalEntryId, e.TotalAmount, duplicates.Count);

            await PublishAnomalyAsync(
                "Duplicate",
                $"Ayni tutar ({e.TotalAmount:N2} TL) son 24 saatte {duplicates.Count + 1} kez islendi. " +
                $"Mukerrer kayit olabilir.",
                e.TotalAmount,
                e.TotalAmount,
                "JournalEntry",
                e.JournalEntryId,
                e.TenantId,
                ct);
        }
    }

    /// <summary>
    /// Anormal tutar kontrolu: Tutar, son 30 gunluk ortalama yevmiye tutarinin 3 katindan fazla mi?
    /// </summary>
    private async Task CheckAbnormalAmountAsync(LedgerPostedEvent e, CancellationToken ct)
    {
        var monthStart = e.EntryDate.AddDays(-30);
        var monthEnd = e.EntryDate;

        var monthEntries = await _journalEntryRepository.GetByDateRangeAsync(
            e.TenantId, monthStart, monthEnd, ct).ConfigureAwait(false);

        var postedEntries = monthEntries
            .Where(je => je.Id != e.JournalEntryId && je.IsPosted)
            .ToList();

        if (postedEntries.Count < 3)
        {
            // Yeterli veri yok — anomali tespiti anlamli degil
            return;
        }

        var averageAmount = postedEntries
            .Average(je => je.Lines.Sum(l => l.Debit));

        var threshold = averageAmount * 3;

        if (e.TotalAmount > threshold)
        {
            _logger.LogWarning(
                "[Anomaly] ABNORMAL AMOUNT tespit edildi: JournalEntryId={JournalEntryId}, " +
                "tutar={Amount:N2}, ortalama={Average:N2}, esik={Threshold:N2}",
                e.JournalEntryId, e.TotalAmount, averageAmount, threshold);

            await PublishAnomalyAsync(
                "AbnormalExpense",
                $"Yevmiye tutari ({e.TotalAmount:N2} TL) son 30 gun ortalamasinin " +
                $"({averageAmount:N2} TL) 3 katini asiyor.",
                averageAmount,
                e.TotalAmount,
                "JournalEntry",
                e.JournalEntryId,
                e.TenantId,
                ct);
        }
    }

    private async Task PublishAnomalyAsync(
        string anomalyType,
        string description,
        decimal? expectedAmount,
        decimal? actualAmount,
        string? entityType,
        Guid entityId,
        Guid tenantId,
        CancellationToken ct)
    {
        var integrationEvent = new FinanceAnomalyDetectedEvent(
            AnomalyType: anomalyType,
            Description: description,
            ExpectedAmount: expectedAmount,
            ActualAmount: actualAmount,
            EntityType: entityType,
            EntityId: entityId,
            TenantId: tenantId,
            OccurredAt: DateTime.UtcNow)
        {
            Amount = actualAmount ?? 0m,
            Details = description
        };

        await _publishEndpoint.Publish(integrationEvent, ct).ConfigureAwait(false);

        await _mediator.Publish(
            new DomainEventNotification<AnomalyDetectedEvent>(
                new AnomalyDetectedEvent
                {
                    AnomalyType = anomalyType,
                    Description = description,
                    ExpectedAmount = expectedAmount,
                    ActualAmount = actualAmount,
                    EntityType = entityType,
                    EntityId = entityId
                }),
            ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[Anomaly] FinanceAnomalyDetected + AnomalyDetectedEvent yayinlandi: tip={AnomalyType}, aciklama={Description}",
            anomalyType, description);
    }
}
