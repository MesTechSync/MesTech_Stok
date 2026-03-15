using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI.Accounting;

// ── Interface ──

/// <summary>
/// Ses kaydini MESA STT ile metne donusturur, ardindan
/// IMesaAccountingService ile yapisal gider verisi cikarir.
/// MUH-03 DEV 4.
/// </summary>
public interface ISpeechToExpenseService
{
    Task<IReadOnlyList<PendingExpense>> ProcessAudioAsync(
        byte[] audioData,
        string mimeType,
        Guid tenantId,
        CancellationToken ct = default);
}

/// <summary>
/// Ses kaydından çıkarılan bekleyen gider kaydı.
/// </summary>
public record PendingExpense(string Title, decimal Amount, string Category, decimal Confidence);

// ── Implementation ──

/// <summary>
/// MESA STT endpoint'ini (localhost:5101) kullanarak ses → metin → gider akisi.
/// MESA kapaninca bos liste doner (Demir Kural #12 — bagimsizlik).
/// </summary>
public class SpeechToExpenseService : ISpeechToExpenseService
{
    private readonly HttpClient _httpClient;
    private readonly IMesaAccountingService _accountingService;
    private readonly IAccountingDocumentRepository _documentRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SpeechToExpenseService> _logger;

    public SpeechToExpenseService(
        HttpClient httpClient,
        IMesaAccountingService accountingService,
        IAccountingDocumentRepository documentRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<SpeechToExpenseService> logger)
    {
        _httpClient = httpClient;
        _accountingService = accountingService;
        _documentRepository = documentRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;

        _httpClient.BaseAddress ??= new Uri("http://localhost:5101");
    }

    public async Task<IReadOnlyList<PendingExpense>> ProcessAudioAsync(
        byte[] audioData,
        string mimeType,
        Guid tenantId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(audioData);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        _logger.LogInformation(
            "[SpeechToExpense] Ses isleme basliyor: boyut={Size} byte, mimeType={MimeType}, tenant={TenantId}",
            audioData.Length, mimeType, tenantId);

        // 1. STT — ses → metin
        string transcribedText;
        decimal sttConfidence;

        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(audioData), "audio", "recording");
            content.Add(new StringContent(mimeType), "mimeType");

            var response = await _httpClient.PostAsync("/api/v1/stt/transcribe", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[SpeechToExpense] MESA STT yanit vermedi: {StatusCode}. Bos liste donuyor.",
                    response.StatusCode);
                return Array.Empty<PendingExpense>();
            }

            var sttResult = await response.Content.ReadFromJsonAsync<SttResponse>(cancellationToken: ct);
            if (sttResult is null || string.IsNullOrWhiteSpace(sttResult.Text))
            {
                _logger.LogWarning("[SpeechToExpense] STT bos metin dondu");
                return Array.Empty<PendingExpense>();
            }

            transcribedText = sttResult.Text;
            sttConfidence = sttResult.Confidence;

            _logger.LogInformation(
                "[SpeechToExpense] STT tamamlandi: guven={Confidence:P0}, metin uzunlugu={Length}",
                sttConfidence, transcribedText.Length);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex,
                "[SpeechToExpense] MESA STT erisime kapali (Demir Kural #12). Bos liste donuyor.");
            return Array.Empty<PendingExpense>();
        }

        // 2. Metin → yapisal veri cikarma (IMesaAccountingService.ExtractDataAsync)
        var textBytes = System.Text.Encoding.UTF8.GetBytes(transcribedText);
        var classification = new DocumentClassification("Receipt", sttConfidence, transcribedText);

        DocumentExtraction extraction;
        try
        {
            extraction = await _accountingService.ExtractDataAsync(textBytes, classification, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[SpeechToExpense] Veri cikarma basarisiz, bos liste donuyor");
            return Array.Empty<PendingExpense>();
        }

        // 3. PendingExpense listesi olustur
        var results = new List<PendingExpense>();

        if (extraction.Amount.HasValue && extraction.Amount.Value > 0)
        {
            var title = extraction.CounterpartyName ?? "Sesli gider";
            var category = extraction.ExtraFields.GetValueOrDefault("category", "Genel");

            results.Add(new PendingExpense(
                title,
                extraction.Amount.Value,
                category,
                sttConfidence));
        }

        // Birden fazla kalem (ExtraFields icinde items varsa)
        if (extraction.ExtraFields.TryGetValue("items", out var itemsJson))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<SttExpenseItem>>(itemsJson);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        results.Add(new PendingExpense(
                            item.Title ?? "Sesli gider kalemi",
                            item.Amount,
                            item.Category ?? "Genel",
                            sttConfidence * 0.9m));
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex,
                    "[SpeechToExpense] Items JSON parse hatasi, tek kalem olarak devam ediyor");
            }
        }

        // 4. Her kalem icin AccountingDocument olustur (Status: PendingApproval)
        foreach (var expense in results)
        {
            var doc = AccountingDocument.Create(
                tenantId: tenantId,
                fileName: $"speech-expense-{DateTime.UtcNow:yyyyMMddHHmmss}.txt",
                mimeType: "text/plain",
                fileSize: textBytes.Length,
                storagePath: $"speech/{tenantId}/{Guid.NewGuid()}.txt",
                documentType: DocumentType.Receipt,
                documentSource: DocumentSource.Scanner,
                amount: expense.Amount,
                extractedData: JsonSerializer.Serialize(new
                {
                    expense.Title,
                    expense.Category,
                    expense.Confidence,
                    TranscribedText = transcribedText
                }));

            await _documentRepository.AddAsync(doc, ct);
        }

        if (results.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "[SpeechToExpense] Islem tamamlandi: {Count} gider kalemi cikarildi",
            results.Count);

        return results;
    }
}

// ── STT Response DTO ──

internal record SttResponse
{
    public string Text { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
}

internal record SttExpenseItem
{
    public string? Title { get; init; }
    public decimal Amount { get; init; }
    public string? Category { get; init; }
}

// ── Mock Implementation ──

/// <summary>
/// Mock SpeechToExpenseService — MESA olmadan test icin.
/// Sabit sahte veri doner.
/// </summary>
public class MockSpeechToExpenseService : ISpeechToExpenseService
{
    private readonly ILogger<MockSpeechToExpenseService> _logger;

    public MockSpeechToExpenseService(ILogger<MockSpeechToExpenseService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<PendingExpense>> ProcessAudioAsync(
        byte[] audioData,
        string mimeType,
        Guid tenantId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK SpeechToExpense] Ses isleme istendi: boyut={Size} byte. Mock veri donuyor.",
            audioData.Length);

        IReadOnlyList<PendingExpense> mockResults = new List<PendingExpense>
        {
            new("Mock gider — ofis malzemesi", 250m, "Ofis", 0.85m),
            new("Mock gider — kargo", 75m, "Lojistik", 0.80m)
        };

        return Task.FromResult(mockResults);
    }
}
