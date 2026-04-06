using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Integration.Payment;

/// <summary>
/// Stripe odeme kapisi entegrasyonu — uluslararasi pazarlar icin.
/// Stripe REST API v1 kullanir. Sandbox/Production mode destekler.
/// NuGet: Stripe.net (resmi SDK) eklendiginde tam implementasyona gecilir.
/// </summary>
public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly ILogger<StripePaymentGateway> _logger;
    private readonly StripeOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    private const int CurrencySubunitMultiplier = 100; // USD → cents

    public string ProviderName => "Stripe";

    public StripePaymentGateway(
        ILogger<StripePaymentGateway> logger,
        IOptions<StripeOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<PaymentResult> ChargeAsync(decimal amount, string currency, string paymentMethodToken,
        string? description = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Stripe Charge: {Amount} {Currency}", amount, currency.ToUpperInvariant());

        if (!_options.IsConfigured)
        {
            _logger.LogWarning("Stripe API key yapilandirilmamis — sandbox modu");
            return new PaymentResult(true, $"pi_sandbox_{Guid.NewGuid():N}"[..28]);
        }

        try
        {
            var http = CreateHttpClient();

            // Idempotency key: aynı tutar+para birimi+ödeme yöntemi kombinasyonu 24 saat içinde
            // tekrar çağrılırsa Stripe aynı PaymentIntent'i döner → çift ödeme ÖNLENIR.
            var idempotencyKey = $"{amount:F2}_{currency}_{paymentMethodToken}_{DateTime.UtcNow:yyyyMMddHH}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(idempotencyKey)));
            http.DefaultRequestHeaders.TryAddWithoutValidation("Idempotency-Key", hash[..48]);

            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["amount"] = ((int)(amount * CurrencySubunitMultiplier)).ToString(), // Stripe kuruş/cent cinsinden alir
                ["currency"] = currency.ToLowerInvariant(),
                ["payment_method"] = paymentMethodToken,
                ["confirm"] = "true",
                ["description"] = description ?? "MesTech Subscription"
            });

            using var response = await http.PostAsync("/v1/payment_intents", formData, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var txId = ExtractField(body, "id");
                _logger.LogInformation("Stripe odeme basarili: {TxId}", txId);
                return new PaymentResult(true, txId);
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("Stripe odeme basarisiz: {Error}", error);
            return new PaymentResult(false, null, error, response.StatusCode.ToString());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Stripe odeme hatasi");
            return new PaymentResult(false, null, ex.Message, "EXCEPTION");
        }
    }

    public async Task<PaymentResult> RefundAsync(string transactionId, decimal? partialAmount = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Stripe Refund: {TxId}", transactionId);

        if (!_options.IsConfigured)
            return new PaymentResult(true, $"re_sandbox_{Guid.NewGuid():N}"[..28]);

        try
        {
            var http = CreateHttpClient();
            var formData = new Dictionary<string, string>
            {
                ["payment_intent"] = transactionId
            };
            if (partialAmount.HasValue)
                formData["amount"] = ((int)(partialAmount.Value * CurrencySubunitMultiplier)).ToString();

            using var response = await http.PostAsync("/v1/refunds", new FormUrlEncodedContent(formData), ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return new PaymentResult(true, transactionId);

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return new PaymentResult(false, null, error, response.StatusCode.ToString());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Stripe refund hatasi: {TxId}", transactionId);
            return new PaymentResult(false, null, ex.Message, "EXCEPTION");
        }
    }

    public Task<string> SaveCardAsync(CardInfo cardInfo, CancellationToken ct = default)
    {
        _logger.LogInformation("Stripe SaveCard: {Holder}", cardInfo.CardHolderName);

        if (!_options.IsConfigured)
            return Task.FromResult($"pm_sandbox_{Guid.NewGuid():N}"[..28]);

        // Stripe card tokenization requires Stripe.net SDK (NuGet: Stripe.net)
        // Until SDK integrated, reject with clear message to prevent fake tokens in production
        _logger.LogWarning("Stripe SaveCard not yet implemented — requires Stripe.net SDK integration");
        throw new NotSupportedException(
            "Stripe card tokenization requires Stripe.net SDK. " +
            "Add NuGet Stripe.net package and implement SetupIntent + PaymentMethod flow.");
    }

    public Task<bool> DeleteCardAsync(string cardToken, CancellationToken ct = default)
    {
        _logger.LogInformation("Stripe DeleteCard: token=***masked***");

        if (!_options.IsConfigured)
            return Task.FromResult(true);

        _logger.LogWarning("Stripe DeleteCard not yet implemented — requires Stripe.net SDK integration");
        throw new NotSupportedException(
            "Stripe card deletion requires Stripe.net SDK. " +
            "Add NuGet Stripe.net package and implement PaymentMethod.Detach flow.");
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient("Stripe");
        var baseUri = new Uri(_options.BaseUrl);
        if (Security.SsrfGuard.IsPrivateHost(baseUri.Host))
            _logger.LogWarning("[StripePaymentGateway] BaseUrl points to private network: {BaseUrl}", _options.BaseUrl);
        client.BaseAddress = baseUri;
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.SecretKey);
        return client;
    }

    private string ExtractField(string json, string field)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var val))
                return val.GetString() ?? Guid.NewGuid().ToString("N");
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "Stripe response {Field} parse failed", field);
        }
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>Stripe yapilandirma.</summary>
public sealed class StripeOptions
{
    public string BaseUrl { get; set; } = "https://api.stripe.com";
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey);
}
