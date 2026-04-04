using System.Net.Http.Json;
using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// MESA OS REST API gercek entegrasyon.
/// Feature flag: Mesa:BridgeEnabled=true olunca Mock yerine bu kullanilir.
/// </summary>
public sealed class RealMesaEventPublisher : IMesaEventPublisher
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<RealMesaEventPublisher> _logger;

    public RealMesaEventPublisher(HttpClient httpClient,
        IConfiguration config, ILogger<RealMesaEventPublisher> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    private async Task PostEventAsync(string eventType, object data, CancellationToken ct)
    {
        var mesaEndpoint = _config["Mesa:BaseUrl"] ?? "http://localhost:3000";
        var apiKey = _config["Mesa:ApiKey"] ?? string.Empty;
        try
        {
            var payload = new { type = eventType, data };
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{mesaEndpoint}/api/v1/events");
            request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
            request.Content = JsonContent.Create(payload);
            using var resp = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("MESA {Type} HTTP {Status} — event delivery failed", eventType, (int)resp.StatusCode);
            }
            else
            {
                _logger.LogInformation("MESA event gonderildi: {Type}", eventType);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogError(ex, "MESA endpoint erisilemedi — {Type} event teslim edilemedi", eventType);
        }
    }

    public Task PublishProductCreatedAsync(MesaProductCreatedEvent evt, CancellationToken ct = default)
        => PostEventAsync("product.created", evt, ct);

    public Task PublishStockLowAsync(MesaStockLowEvent evt, CancellationToken ct = default)
        => PostEventAsync("stock.low", evt, ct);

    public Task PublishOrderReceivedAsync(MesaOrderReceivedEvent evt, CancellationToken ct = default)
        => PostEventAsync("order.received", evt, ct);

    public Task PublishPriceChangedAsync(MesaPriceChangedEvent evt, CancellationToken ct = default)
        => PostEventAsync("price.changed", evt, ct);

    public Task PublishInvoiceGeneratedAsync(MesaInvoiceGeneratedEvent evt, CancellationToken ct = default)
        => PostEventAsync("invoice.generated", evt, ct);

    public Task PublishInvoiceCancelledAsync(MesaInvoiceCancelledEvent evt, CancellationToken ct = default)
        => PostEventAsync("invoice.cancelled", evt, ct);

    public Task PublishReturnCreatedAsync(MesaReturnCreatedEvent evt, CancellationToken ct = default)
        => PostEventAsync("return.created", evt, ct);

    public Task PublishReturnResolvedAsync(MesaReturnResolvedEvent evt, CancellationToken ct = default)
        => PostEventAsync("return.resolved", evt, ct);

    public Task PublishBuyboxLostAsync(MesaBuyboxLostEvent evt, CancellationToken ct = default)
        => PostEventAsync("buybox.lost", evt, ct);

    public Task PublishSupplierFeedSyncedAsync(MesaSupplierFeedSyncedEvent evt, CancellationToken ct = default)
        => PostEventAsync("supplier.feed.synced", evt, ct);

    public Task PublishDailySummaryAsync(MesaDailySummaryEvent evt, CancellationToken ct = default)
        => PostEventAsync("daily.summary", evt, ct);

    public Task PublishSyncErrorAsync(MesaSyncErrorEvent evt, CancellationToken ct = default)
        => PostEventAsync("sync.error", evt, ct);

    public Task PublishLeadConvertedAsync(LeadConvertedIntegrationEvent @event, CancellationToken ct = default)
        => PostEventAsync("crm.lead.converted", @event, ct);

    public Task PublishDealWonAsync(DealWonIntegrationEvent @event, CancellationToken ct = default)
        => PostEventAsync("crm.deal.won", @event, ct);

    public Task PublishDealLostAsync(DealLostIntegrationEvent @event, CancellationToken ct = default)
        => PostEventAsync("crm.deal.lost", @event, ct);

    public Task RequestLeadScoringAsync(Guid leadId, Guid tenantId, string fullName,
        string? company, string source, CancellationToken ct = default)
        => PostEventAsync("ai.lead.scoring.request",
            new { leadId, tenantId, fullName, company, source }, ct);

    public Task PublishLeaveApprovedAsync(Guid leaveId, Guid employeeId,
        DateTime occurredAt, CancellationToken ct = default)
        => PostEventAsync("hr.leave.approved",
            new { leaveId, employeeId, occurredAt }, ct);

    public Task PublishBankImportedAsync(Accounting.Events.FinanceBankImportedEvent evt, CancellationToken ct = default)
        => PostEventAsync("finance.bank.imported", evt, ct);

    public Task PublishLedgerPostedAsync(Accounting.Events.FinanceLedgerPostedEvent evt, CancellationToken ct = default)
        => PostEventAsync("finance.ledger.posted", evt, ct);
}
