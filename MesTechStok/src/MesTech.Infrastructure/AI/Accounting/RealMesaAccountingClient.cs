using System.Net.Http.Json;
using MesTech.Application.Interfaces.Accounting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace MesTech.Infrastructure.AI.Accounting;

/// <summary>
/// MESA OS Muhasebe AI servisi — gercek HTTP API cagrisi yapar.
/// Feature flag: Mesa:Accounting:UseReal=true olunca Mock yerine bu kullanilir.
/// Demir Kural #12: MESA kopunca MockMesaAccountingService'e fallback yapar.
/// Endpoint: localhost:3101 (MESA Status port).
/// </summary>
public sealed class RealMesaAccountingClient : IMesaAccountingService
{
    private readonly HttpClient _httpClient;
    private readonly MockMesaAccountingService _mockFallback;
    private readonly ILogger<RealMesaAccountingClient> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public RealMesaAccountingClient(
        HttpClient httpClient,
        IConfiguration configuration,
        MockMesaAccountingService mockFallback,
        ILogger<RealMesaAccountingClient> logger)
    {
        _httpClient = httpClient;
        _mockFallback = mockFallback;
        _logger = logger;

        var baseUrl = configuration["Mesa:Accounting:BaseUrl"] ?? "http://localhost:3101";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(configuration.GetValue<int>("Mesa:Accounting:TimeoutSeconds", 30));

        _circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<OperationCanceledException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(45),
                onBreak: (ex, ts) => { MesaMetrics.RecordCircuitState("mesa_accounting", 2); _logger.LogWarning(
                    "[MESA Accounting] Circuit OPEN — {Duration}s. Error: {Error}",
                    ts.TotalSeconds, ex.Message); },
                onReset: () => { MesaMetrics.RecordCircuitState("mesa_accounting", 0); _logger.LogInformation(
                    "[MESA Accounting] Circuit CLOSED — baglanti yeniden aktif"); },
                onHalfOpen: () => { MesaMetrics.RecordCircuitState("mesa_accounting", 1); _logger.LogInformation(
                    "[MESA Accounting] Circuit HALF-OPEN — test cagrisi yapiliyor"); });
    }

    public async Task<DocumentClassification> ClassifyDocumentAsync(
        byte[] fileData, string mimeType, CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                using var content = new MultipartFormDataContent();
                content.Add(new ByteArrayContent(fileData), "file", "document");
                content.Add(new StringContent(mimeType), "mimeType");

                using var response = await _httpClient.PostAsync("/api/v1/accounting/classify", content, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA Real] Classify failed: {StatusCode}", response.StatusCode);
                    return await _mockFallback.ClassifyDocumentAsync(fileData, mimeType, ct).ConfigureAwait(false);
                }

                var result = await response.Content.ReadFromJsonAsync<MesaClassifyResponse>(
                    cancellationToken: ct).ConfigureAwait(false);

                if (result is null)
                {
                    _logger.LogWarning("[MESA Real] Classify response deserialization failed");
                    return await _mockFallback.ClassifyDocumentAsync(fileData, mimeType, ct).ConfigureAwait(false);
                }

                var confidence = Math.Clamp(result.Confidence, 0m, 1m);
                if (confidence != result.Confidence)
                {
                    _logger.LogWarning(
                        "[MESA Real] Classify confidence out of range: {Raw} → clamped to {Clamped}",
                        result.Confidence, confidence);
                }

                _logger.LogInformation(
                    "[MESA Real] Classify basarili: tip={DocumentType}, guven={Confidence:P0}",
                    result.Type, confidence);

                return new DocumentClassification(result.Type, confidence, result.RawText ?? "");
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex, "[MESA Real] MESA OS unreachable, falling back to mock (classify)");
            return await _mockFallback.ClassifyDocumentAsync(fileData, mimeType, ct).ConfigureAwait(false);
        }
    }

    public async Task<DocumentExtraction> ExtractDataAsync(
        byte[] fileData, DocumentClassification classification, CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                using var content = new MultipartFormDataContent();
                content.Add(new ByteArrayContent(fileData), "file", "document");
                content.Add(new StringContent(classification.DocumentType), "documentType");
                content.Add(new StringContent(classification.Confidence.ToString("F2",
                    System.Globalization.CultureInfo.InvariantCulture)), "confidence");

                using var response = await _httpClient.PostAsync("/api/v1/accounting/extract", content, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA Real] Extract failed: {StatusCode}", response.StatusCode);
                    return await _mockFallback.ExtractDataAsync(fileData, classification, ct).ConfigureAwait(false);
                }

                var result = await response.Content.ReadFromJsonAsync<MesaExtractResponse>(
                    cancellationToken: ct).ConfigureAwait(false);

                if (result is null)
                {
                    _logger.LogWarning("[MESA Real] Extract response deserialization failed");
                    return await _mockFallback.ExtractDataAsync(fileData, classification, ct).ConfigureAwait(false);
                }

                _logger.LogInformation(
                    "[MESA Real] Extract basarili: tutar={Amount}, firma={Counterparty}",
                    result.Amount, result.CounterpartyName ?? "-");

                return new DocumentExtraction(
                    result.Amount,
                    result.TaxAmount,
                    result.CounterpartyName,
                    result.VKN,
                    result.Date,
                    result.ExtraFields ?? new Dictionary<string, string>());
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex, "[MESA Real] MESA OS unreachable, falling back to mock (extract)");
            return await _mockFallback.ExtractDataAsync(fileData, classification, ct).ConfigureAwait(false);
        }
    }

    public async Task<ReconciliationSuggestion> SuggestReconciliationAsync(
        Guid settlementBatchId, IReadOnlyList<Guid> candidateBankTransactionIds,
        CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var payload = new
                {
                    settlementBatchId,
                    candidateBankTransactionIds
                };

                using var response = await _httpClient.PostAsJsonAsync(
                    "/api/v1/accounting/reconcile/suggest", payload, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA Real] Reconciliation suggest failed: {StatusCode}", response.StatusCode);
                    return await _mockFallback.SuggestReconciliationAsync(
                        settlementBatchId, candidateBankTransactionIds, ct).ConfigureAwait(false);
                }

                var result = await response.Content.ReadFromJsonAsync<MesaReconciliationResponse>(
                    cancellationToken: ct).ConfigureAwait(false);

                if (result is null)
                {
                    _logger.LogWarning("[MESA Real] Reconciliation response deserialization failed");
                    return await _mockFallback.SuggestReconciliationAsync(
                        settlementBatchId, candidateBankTransactionIds, ct).ConfigureAwait(false);
                }

                var confidence = Math.Clamp(result.Confidence, 0m, 1m);
                if (confidence != result.Confidence)
                {
                    _logger.LogWarning(
                        "[MESA Real] Reconciliation confidence out of range: {Raw} → clamped to {Clamped}",
                        result.Confidence, confidence);
                }

                _logger.LogInformation(
                    "[MESA Real] Reconciliation basarili: batch={BatchId}, tx={TxId}, guven={Confidence:P0}",
                    result.SettlementBatchId, result.BankTransactionId, confidence);

                return new ReconciliationSuggestion(
                    result.SettlementBatchId,
                    result.BankTransactionId,
                    confidence,
                    result.Reason ?? "MESA AI match");
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex, "[MESA Real] MESA OS unreachable, falling back to mock (reconcile)");
            return await _mockFallback.SuggestReconciliationAsync(
                settlementBatchId, candidateBankTransactionIds, ct).ConfigureAwait(false);
        }
    }
}

// ── MESA Response DTOs ──

/// <summary>
/// MESA OS /api/v1/accounting/classify yanit modeli.
/// </summary>
public record MesaClassifyResponse
{
    public string Type { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
    public string? RawText { get; init; }
}

/// <summary>
/// MESA OS /api/v1/accounting/extract yanit modeli.
/// </summary>
public record MesaExtractResponse
{
    public decimal? Amount { get; init; }
    public decimal? TaxAmount { get; init; }
    public string? CounterpartyName { get; init; }
    public string? VKN { get; init; }
    public DateTime? Date { get; init; }
    public Dictionary<string, string>? ExtraFields { get; init; }
}

/// <summary>
/// MESA OS /api/v1/accounting/reconcile/suggest yanit modeli.
/// </summary>
public record MesaReconciliationResponse
{
    public Guid SettlementBatchId { get; init; }
    public Guid BankTransactionId { get; init; }
    public decimal Confidence { get; init; }
    public string? Reason { get; init; }
}
