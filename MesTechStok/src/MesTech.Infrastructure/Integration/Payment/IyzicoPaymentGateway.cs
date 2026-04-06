using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Integration.Payment;

/// <summary>
/// iyzico odeme kapisi entegrasyonu — Turkiye pazari icin birincil gateway.
/// iyzico REST API v2 kullanir. Sandbox/Production mode destekler.
/// NuGet: Iyzipay (resmi SDK) eklendiginde tam implementasyona gecilir.
/// </summary>
public sealed class IyzicoPaymentGateway : IPaymentGateway
{
    private readonly ILogger<IyzicoPaymentGateway> _logger;
    private readonly IyzicoOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public string ProviderName => "iyzico";

    public IyzicoPaymentGateway(
        ILogger<IyzicoPaymentGateway> logger,
        IOptions<IyzicoOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<PaymentResult> ChargeAsync(decimal amount, string currency, string paymentMethodToken,
        string? description = null, CancellationToken ct = default)
    {
        _logger.LogInformation("iyzico Charge: {Amount} {Currency} token=***masked***", amount, currency);

        if (!_options.IsConfigured)
        {
            _logger.LogWarning("iyzico API key yapılandırılmamış — sandbox modu");
            return new PaymentResult(true, $"sandbox-{Guid.NewGuid():N}"[..24]);
        }

        try
        {
            // iyzico REST API v2 cagirisi
            var http = CreateHttpClient();

            // Idempotency: conversationId'yi ödeme parametrelerinden türet.
            // Aynı tutar+token+saat kombinasyonu → aynı conversationId → iyzico çift ödeme ÖNLER.
            var idempotencyInput = $"{amount:F2}_{currency}_{paymentMethodToken}_{DateTime.UtcNow:yyyyMMddHH}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            var conversationId = Convert.ToHexString(
                sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(idempotencyInput)))[..20];

            var payload = new
            {
                locale = "tr",
                conversationId,
                price = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                paidPrice = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                currency,
                paymentCard = new { cardToken = paymentMethodToken },
                basketItems = new[]
                {
                    new
                    {
                        id = "SUBSCRIPTION",
                        name = description ?? "MesTech Abonelik",
                        category1 = "SaaS",
                        itemType = "VIRTUAL",
                        price = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            using var response = await http.PostAsync("/payment/auth", content, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var txId = ExtractTransactionId(body);
                _logger.LogInformation("iyzico odeme basarili: {TxId}", txId);
                return new PaymentResult(true, txId);
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("iyzico odeme basarisiz: {Error}", error);
            return new PaymentResult(false, null, error, response.StatusCode.ToString());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "iyzico odeme hatasi");
            return new PaymentResult(false, null, ex.Message, "EXCEPTION");
        }
    }

    public async Task<PaymentResult> RefundAsync(string transactionId, decimal? partialAmount = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("iyzico Refund: {TxId} amount={Amount}", transactionId, partialAmount?.ToString("F2") ?? "full");

        if (!_options.IsConfigured)
            return new PaymentResult(true, $"refund-sandbox-{Guid.NewGuid():N}"[..24]);

        try
        {
            var http = CreateHttpClient();
            var payload = new
            {
                locale = "tr",
                conversationId = Guid.NewGuid().ToString("N")[..20],
                paymentTransactionId = transactionId,
                price = partialAmount?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            using var response = await http.PostAsync("/payment/refund", content, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return new PaymentResult(true, transactionId);

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return new PaymentResult(false, null, error, response.StatusCode.ToString());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "iyzico refund hatasi: {TxId}", transactionId);
            return new PaymentResult(false, null, ex.Message, "EXCEPTION");
        }
    }

    public Task<string> SaveCardAsync(CardInfo cardInfo, CancellationToken ct = default)
    {
        _logger.LogInformation("iyzico SaveCard: {Holder}", cardInfo.CardHolderName);

        if (!_options.IsConfigured)
            return Task.FromResult($"sandbox-token-{Guid.NewGuid():N}"[..32]);

        // iyzico card tokenization requires iyzipay SDK (NuGet: Iyzipay)
        // Until SDK integrated, reject with clear message to prevent fake tokens in production
        _logger.LogWarning("iyzico SaveCard not yet implemented — requires Iyzipay SDK integration");
        throw new NotSupportedException(
            "iyzico card tokenization requires Iyzipay SDK. " +
            "Add NuGet Iyzipay package and implement CreateCardRequest flow.");
    }

    public Task<bool> DeleteCardAsync(string cardToken, CancellationToken ct = default)
    {
        _logger.LogInformation("iyzico DeleteCard: token=***masked***");

        if (!_options.IsConfigured)
            return Task.FromResult(true);

        _logger.LogWarning("iyzico DeleteCard not yet implemented — requires Iyzipay SDK integration");
        throw new NotSupportedException(
            "iyzico card deletion requires Iyzipay SDK. " +
            "Add NuGet Iyzipay package and implement DeleteCardRequest flow.");
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient("Iyzico");
        var baseUri = new Uri(_options.BaseUrl);
        if (Security.SsrfGuard.IsPrivateHost(baseUri.Host))
            _logger.LogWarning("[IyzicoPaymentGateway] BaseUrl points to private network: {BaseUrl}", _options.BaseUrl);
        client.BaseAddress = baseUri;
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Add("Authorization", $"IYZWS {_options.ApiKey}");
        return client;
    }

    private string ExtractTransactionId(string responseBody)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("paymentId", out var pid))
                return pid.GetString() ?? Guid.NewGuid().ToString("N");
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "iyzico response paymentId parse failed");
        }
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>iyzico yapilandirma.</summary>
public sealed class IyzicoOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(SecretKey);
}
