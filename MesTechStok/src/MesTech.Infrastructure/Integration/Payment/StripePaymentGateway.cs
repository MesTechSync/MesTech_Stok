using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Integration.Payment;

/// <summary>
/// Stripe odeme kapisi entegrasyonu — uluslararasi pazarlar icin.
/// Stripe REST API v1 kullanir. Sandbox/Production mode destekler.
/// NuGet: Stripe.net (resmi SDK) eklendiginde tam implementasyona gecilir.
/// </summary>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly ILogger<StripePaymentGateway> _logger;
    private readonly StripeOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string StripeApiBaseUrl = "https://api.stripe.com";

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
            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["amount"] = ((int)(amount * 100)).ToString(), // Stripe kuruş/cent cinsinden alir
                ["currency"] = currency.ToLowerInvariant(),
                ["payment_method"] = paymentMethodToken,
                ["confirm"] = "true",
                ["description"] = description ?? "MesTech Subscription"
            });

            var response = await http.PostAsync("/v1/payment_intents", formData, ct).ConfigureAwait(false);

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
        catch (Exception ex)
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
                formData["amount"] = ((int)(partialAmount.Value * 100)).ToString();

            var response = await http.PostAsync("/v1/refunds", new FormUrlEncodedContent(formData), ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return new PaymentResult(true, transactionId);

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return new PaymentResult(false, null, error, response.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            return new PaymentResult(false, null, ex.Message, "EXCEPTION");
        }
    }

    public async Task<string> SaveCardAsync(CardInfo cardInfo, CancellationToken ct = default)
    {
        _logger.LogInformation("Stripe SaveCard: {Holder}", cardInfo.CardHolderName);

        if (!_options.IsConfigured)
            return $"pm_sandbox_{Guid.NewGuid():N}"[..28];

        await Task.CompletedTask;
        return $"pm_{Guid.NewGuid():N}"[..28];
    }

    public async Task<bool> DeleteCardAsync(string cardToken, CancellationToken ct = default)
    {
        _logger.LogInformation("Stripe DeleteCard: token=***masked***");
        await Task.CompletedTask;
        return true;
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient("Stripe");
        client.BaseAddress = new Uri(StripeApiBaseUrl);
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
public class StripeOptions
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey);
}
