using System.Text;
using MesTech.Application.Interfaces.Accounting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI.Accounting;

/// <summary>
/// Mock MESA Muhasebe AI servisi — gercek API cagrisi yapmaz.
/// Kural tabanli siniflandirma ve sahte veri cikarma yapar.
/// Feature flag: Mesa:Accounting:UseMock (default true).
/// MESA kopunca MesTech calismaya DEVAM eder (12. karar — bagimsizlik).
/// Dalga 2+: RealMesaAccountingClient ile DI'dan swap edilecek.
/// </summary>
public sealed class MockMesaAccountingService : IMesaAccountingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MockMesaAccountingService> _logger;

    public MockMesaAccountingService(
        IConfiguration configuration,
        ILogger<MockMesaAccountingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<DocumentClassification> ClassifyDocumentAsync(
        byte[] fileData, string mimeType, CancellationToken ct = default)
    {
        var useMock = _configuration.GetValue("Mesa:Accounting:UseMock", true);
        if (!useMock)
        {
            _logger.LogWarning(
                "[MOCK] Mesa:Accounting:UseMock=false ama gercek servis henuz yok. Mock kullaniliyor.");
        }

        _logger.LogInformation(
            "[MOCK] Belge siniflandirma istendi: mimeType={MimeType}, boyut={Size} byte",
            mimeType, fileData.Length);

        var rawText = TryExtractText(fileData);

        var classification = ClassifyByRules(mimeType, rawText);

        _logger.LogInformation(
            "[MOCK] Siniflandirma sonucu: tip={DocumentType}, guven={Confidence:P0}",
            classification.DocumentType, classification.Confidence);

        return Task.FromResult(classification);
    }

    public Task<DocumentExtraction> ExtractDataAsync(
        byte[] fileData, DocumentClassification classification, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] Veri cikarma istendi: tip={DocumentType}, guven={Confidence:P0}",
            classification.DocumentType, classification.Confidence);

        var extraction = classification.DocumentType switch
        {
            "Invoice" => new DocumentExtraction(
                Amount: 1000m,
                TaxAmount: 180m,
                CounterpartyName: "Mock Firma",
                VKN: "1234567890",
                Date: DateTime.UtcNow.Date,
                ExtraFields: new Dictionary<string, string>
                {
                    ["invoiceNumber"] = $"MF-{DateTime.UtcNow:yyyyMMdd}-001",
                    ["currency"] = "TRY"
                }),

            "Receipt" => new DocumentExtraction(
                Amount: 150m,
                TaxAmount: null,
                CounterpartyName: null,
                VKN: null,
                Date: DateTime.UtcNow.Date,
                ExtraFields: new Dictionary<string, string>
                {
                    ["receiptType"] = "Perakende"
                }),

            _ => new DocumentExtraction(
                Amount: null,
                TaxAmount: null,
                CounterpartyName: null,
                VKN: null,
                Date: DateTime.UtcNow.Date,
                ExtraFields: new Dictionary<string, string>
                {
                    ["note"] = "Otomatik veri cikarma yapilamadi"
                })
        };

        _logger.LogInformation(
            "[MOCK] Veri cikarma sonucu: tutar={Amount}, vergi={Tax}, firma={Firm}",
            extraction.Amount, extraction.TaxAmount, extraction.CounterpartyName ?? "-");

        return Task.FromResult(extraction);
    }

    public Task<ReconciliationSuggestion> SuggestReconciliationAsync(
        Guid settlementBatchId, IReadOnlyList<Guid> candidateBankTransactionIds,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] Mutabakat onerisi istendi: batch={BatchId}, aday sayisi={Count}",
            settlementBatchId, candidateBankTransactionIds.Count);

        if (candidateBankTransactionIds.Count == 0)
        {
            _logger.LogWarning(
                "[MOCK] Aday banka hareketi yok — bos oneri donuyor.");

            return Task.FromResult(new ReconciliationSuggestion(
                settlementBatchId,
                Guid.Empty,
                0m,
                "Mock: Aday banka hareketi bulunamadi"));
        }

        var selectedId = candidateBankTransactionIds[0];

        var suggestion = new ReconciliationSuggestion(
            settlementBatchId,
            selectedId,
            0.75m,
            "Mock: amount range match");

        _logger.LogInformation(
            "[MOCK] Mutabakat onerisi: batch={BatchId} <-> transaction={TransactionId}, guven={Confidence:P0}",
            suggestion.SettlementBatchId, suggestion.BankTransactionId, suggestion.Confidence);

        return Task.FromResult(suggestion);
    }

    // -- Private Helpers --

    private static string TryExtractText(byte[] fileData)
    {
        try
        {
            return Encoding.UTF8.GetString(fileData);
        }
        catch
        {
            // Intentional: binary file — text cikarma basarisiz
            return string.Empty;
        }
    }

    private static DocumentClassification ClassifyByRules(string mimeType, string rawText)
    {
        var upperText = rawText.ToUpperInvariant();

        if (mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            if (upperText.Contains("FATURA"))
                return new DocumentClassification("Invoice", 0.85m, rawText);

            if (upperText.Contains("EKSTRE") || upperText.Contains("HESAP OZETI"))
                return new DocumentClassification("BankStatement", 0.80m, rawText);

            return new DocumentClassification("Other", 0.50m, rawText);
        }

        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return new DocumentClassification("Receipt", 0.70m, rawText);
        }

        return new DocumentClassification("Other", 0.50m, rawText);
    }
}
