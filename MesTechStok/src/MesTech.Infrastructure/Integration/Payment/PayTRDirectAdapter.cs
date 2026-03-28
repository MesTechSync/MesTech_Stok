using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Payment;

/// <summary>
/// PayTR Direct API adapter.
/// Auth: HMAC-SHA256 token derived from merchant_id + user_ip + merchant_oid + email +
///       payment_amount + basket JSON + ... + merchant_salt, Base64-encoded.
/// Supports 3D Secure redirect flow, installment BIN query, and refund.
/// Config keys: PayTR:MerchantId, PayTR:MerchantKey, PayTR:MerchantSalt,
///              PayTR:DirectBaseUrl, PayTR:TestMode
/// </summary>
public sealed class PayTRDirectAdapter : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayTRDirectAdapter> _logger;
    private readonly PayTRDirectOptions _options;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private readonly JsonSerializerOptions _jsonOptions;
    private const int CurrencySubunitMultiplier = 100; // TL → kuruş

    private const string TokenEndpoint = "/api/paytrdirect";
    private const string StatusEndpoint = "/odeme/durum";
    private const string RefundEndpoint = "/odeme/iade";
    private const string BinEndpoint = "/odeme/taksit";

    public PaymentProviderType Provider => PaymentProviderType.PayTRDirect;

    public PayTRDirectAdapter(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<PayTRDirectAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options = BindOptions(configuration ?? throw new ArgumentNullException(nameof(configuration)));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        if (!string.IsNullOrEmpty(_options.BaseUrl))
            _httpClient.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);

        _retryPipeline = BuildRetryPipeline();
    }

    // ── IPaymentProvider ─────────────────────────────────────────────────────

    public async Task<PaymentResult> ProcessPaymentAsync(
        PaymentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "[PayTRDirect] ProcessPayment order={OrderId} amount={Amount} ip={Ip}",
            request.OrderId, request.Amount, request.CustomerIp);

        try
        {
            var merchantOid = request.OrderId.ToString("N");
            var basketJson = BuildBasketJson(request.BasketItems);
            var token = GenerateToken(
                merchantOid: merchantOid,
                email: "customer@mestech.app",
                userIp: request.CustomerIp,
                paymentAmount: (long)(request.Amount * CurrencySubunitMultiplier),
                basketJson: basketJson,
                currency: request.Currency,
                testMode: _options.TestMode ? "1" : "0");

            var payload = new Dictionary<string, string>
            {
                ["merchant_id"] = _options.MerchantId,
                ["user_ip"] = request.CustomerIp,
                ["merchant_oid"] = merchantOid,
                ["email"] = "customer@mestech.app",
                ["payment_amount"] = ((long)(request.Amount * CurrencySubunitMultiplier)).ToString(),
                ["paytr_token"] = token,
                ["user_basket"] = basketJson,
                ["debug_on"] = _options.TestMode ? "1" : "0",
                ["no_installment"] = "0",
                ["max_installment"] = "0",
                ["currency"] = request.Currency,
                ["test_mode"] = _options.TestMode ? "1" : "0",
                ["non_3d"] = "0",
                ["merchant_ok_url"] = request.ReturnUrl,
                ["merchant_fail_url"] = request.ReturnUrl
            };

            var response = await ExecuteWithRetryAsync(
                () => BuildFormRequest(TokenEndpoint, payload), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[PayTRDirect] Token request failed {Status}: {Body}", response.StatusCode, body);
                return new PaymentResult(false, null, null, $"PayTR hatasi: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PayTRTokenResponse>(_jsonOptions, ct).ConfigureAwait(false);

            if (result?.Status != "success")
            {
                _logger.LogWarning("[PayTRDirect] Token generation failed: {Reason}", result?.Reason);
                return new PaymentResult(false, null, null, result?.Reason ?? "PayTR token hatasi");
            }

            var redirectUrl = $"{_options.BaseUrl}/odeme?token={result.Token}";

            _logger.LogInformation("[PayTRDirect] 3D redirect URL generated for order {OrderId}", request.OrderId);
            return new PaymentResult(true, merchantOid, redirectUrl, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayTRDirect] ProcessPayment failed for order {OrderId}", request.OrderId);
            return new PaymentResult(false, null, null, ex.Message);
        }
    }

    public async Task<PaymentStatusResult> GetTransactionStatusAsync(
        string transactionId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        _logger.LogInformation("[PayTRDirect] GetTransactionStatus merchantOid={Oid}", transactionId);

        try
        {
            var hashInput = $"{_options.MerchantId}{transactionId}{_options.MerchantSalt}";
            var token = ComputeHmacSha256Base64(hashInput, _options.MerchantKey);

            var payload = new Dictionary<string, string>
            {
                ["merchant_id"] = _options.MerchantId,
                ["merchant_oid"] = transactionId,
                ["paytr_token"] = token
            };

            var response = await ExecuteWithRetryAsync(
                () => BuildFormRequest(StatusEndpoint, payload), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return new PaymentStatusResult(transactionId, PaymentTransactionStatus.Failed, 0m, null);

            var result = await response.Content.ReadFromJsonAsync<PayTRStatusResponse>(_jsonOptions, ct).ConfigureAwait(false);

            var status = result?.Status switch
            {
                "success" => PaymentTransactionStatus.Completed,
                "failed" => PaymentTransactionStatus.Failed,
                "pending" => PaymentTransactionStatus.Pending,
                "refunded" => PaymentTransactionStatus.Refunded,
                _ => PaymentTransactionStatus.Pending
            };

            var amount = result?.TotalAmount is not null
                ? decimal.Parse(result.TotalAmount, CultureInfo.InvariantCulture) / 100m
                : 0m;

            return new PaymentStatusResult(transactionId, status, amount, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayTRDirect] GetTransactionStatus failed for {Oid}", transactionId);
            return new PaymentStatusResult(transactionId, PaymentTransactionStatus.Failed, 0m, null);
        }
    }

    public async Task<InstallmentOptions> GetInstallmentOptionsAsync(
        decimal amount,
        string? binNumber,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[PayTRDirect] GetInstallmentOptions amount={Amount} bin={Bin}", amount, binNumber);

        try
        {
            var hashInput = $"{_options.MerchantId}{binNumber}{(long)(amount * CurrencySubunitMultiplier)}{_options.MerchantSalt}";
            var token = ComputeHmacSha256Base64(hashInput, _options.MerchantKey);

            var payload = new Dictionary<string, string>
            {
                ["merchant_id"] = _options.MerchantId,
                ["card_type"] = binNumber?.Length >= 6 ? binNumber[..6] : string.Empty,
                ["amount"] = ((long)(amount * CurrencySubunitMultiplier)).ToString(),
                ["paytr_token"] = token
            };

            var response = await ExecuteWithRetryAsync(
                () => BuildFormRequest(BinEndpoint, payload), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return new InstallmentOptions(Array.Empty<InstallmentOption>());

            var result = await response.Content.ReadFromJsonAsync<PayTRBinResponse>(_jsonOptions, ct).ConfigureAwait(false);

            if (result?.Status != "success" || result.InstallmentTable is null)
                return new InstallmentOptions(Array.Empty<InstallmentOption>());

            var options = result.InstallmentTable
                .Select(row => new InstallmentOption(
                    Count: row.Count,
                    TotalAmount: row.Total / 100m,
                    MonthlyAmount: row.Monthly / 100m,
                    InterestRate: row.InterestRate))
                .ToList();

            return new InstallmentOptions(options.AsReadOnly());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayTRDirect] GetInstallmentOptions failed");
            return new InstallmentOptions(Array.Empty<InstallmentOption>());
        }
    }

    public async Task<RefundResult> RefundAsync(
        string transactionId,
        decimal amount,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        _logger.LogInformation("[PayTRDirect] Refund merchantOid={Oid} amount={Amount}", transactionId, amount);

        try
        {
            var refundAmount = (long)(amount * CurrencySubunitMultiplier);
            var hashInput = $"{_options.MerchantId}{transactionId}{refundAmount}{_options.MerchantSalt}";
            var token = ComputeHmacSha256Base64(hashInput, _options.MerchantKey);

            var payload = new Dictionary<string, string>
            {
                ["merchant_id"] = _options.MerchantId,
                ["merchant_oid"] = transactionId,
                ["return_amount"] = refundAmount.ToString(),
                ["paytr_token"] = token
            };

            var response = await ExecuteWithRetryAsync(
                () => BuildFormRequest(RefundEndpoint, payload), ct).ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);

            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;

            if (status == "success")
            {
                _logger.LogInformation("[PayTRDirect] Refund successful for {Oid}", transactionId);
                return new RefundResult(true, $"refund-{transactionId}", null);
            }

            var reason = doc.RootElement.TryGetProperty("err_no", out var e) ? e.GetString() : "Bilinmeyen hata";
            _logger.LogWarning("[PayTRDirect] Refund failed for {Oid}: {Reason}", transactionId, reason);
            return new RefundResult(false, null, reason);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayTRDirect] Refund failed for {Oid}", transactionId);
            return new RefundResult(false, null, ex.Message);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Generates PayTR Direct token.
    /// Hash: HMAC-SHA256(merchant_id + user_ip + merchant_oid + email + payment_amount +
    ///       user_basket + no_installment + max_installment + currency + test_mode + merchant_salt)
    /// </summary>
    private string GenerateToken(
        string merchantOid,
        string email,
        string userIp,
        long paymentAmount,
        string basketJson,
        string currency,
        string testMode)
    {
        // PayTR token generation — concatenation order per official documentation
        var raw = string.Concat(
            _options.MerchantId,
            userIp,
            merchantOid,
            email,
            paymentAmount.ToString(),
            basketJson,
            "0",    // no_installment
            "0",    // max_installment
            currency,
            testMode,
            _options.MerchantSalt);

        return ComputeHmacSha256Base64(raw, _options.MerchantKey);
    }

    private static string ComputeHmacSha256Base64(string data, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hash);
    }

    private static string BuildBasketJson(IReadOnlyList<BasketItem>? items)
    {
        if (items is null || items.Count == 0)
            return Convert.ToBase64String(Encoding.UTF8.GetBytes("[]"));

        var basketArray = items.Select(i => new[]
        {
            i.Name,
            i.Price.ToString("F2", CultureInfo.InvariantCulture),
            "1"
        }).ToArray();

        var json = JsonSerializer.Serialize(basketArray);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static HttpRequestMessage BuildFormRequest(string endpoint, Dictionary<string, string> payload)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(payload)
        };
        return req;
    }

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken ct)
    {
        try
        {
            return await _retryPipeline.ExecuteAsync(async token =>
            {
                using var request = requestFactory();
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "[PayTRDirect] Circuit breaker open — returning 503");
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Circuit breaker open")
            };
        }
    }

    private ResiliencePipeline<HttpResponseMessage> BuildRetryPipeline()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                DelayGenerator = args => new ValueTask<TimeSpan?>(
                    TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber))),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning("[PayTRDirect] Retry {Attempt} after {Delay}ms",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
                    .Handle<HttpRequestException>(),
                OnOpened = args =>
                {
                    _logger.LogWarning("[PayTRDirect] Circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    private static PayTRDirectOptions BindOptions(IConfiguration configuration)
    {
        return new PayTRDirectOptions
        {
            MerchantId = configuration["PayTR:MerchantId"] ?? string.Empty,
            MerchantKey = configuration["PayTR:MerchantKey"] ?? string.Empty,
            MerchantSalt = configuration["PayTR:MerchantSalt"] ?? string.Empty,
            BaseUrl = configuration["PayTR:DirectBaseUrl"] ?? "https://www.paytr.com/odeme",
            TestMode = bool.TryParse(configuration["PayTR:TestMode"], out var tm) && tm
        };
    }

    // ── Response models (internal — not part of public API) ──────────────────

    private sealed class PayTRTokenResponse
    {
        public string? Status { get; set; }
        public string? Token { get; set; }
        public string? Reason { get; set; }
    }

    private sealed class PayTRStatusResponse
    {
        public string? Status { get; set; }
        public string? TotalAmount { get; set; }
    }

    private sealed class PayTRBinResponse
    {
        public string? Status { get; set; }
        public List<PayTRInstallmentRow>? InstallmentTable { get; set; }
    }

    private sealed class PayTRInstallmentRow
    {
        public int Count { get; set; }
        public long Total { get; set; }
        public long Monthly { get; set; }
        public decimal InterestRate { get; set; }
    }
}

/// <summary>
/// Strongly-typed PayTR Direct configuration — bound from IConfiguration.
/// No secrets stored in code.
/// </summary>
internal sealed class PayTRDirectOptions
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string MerchantSalt { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.paytr.com/odeme";
    public bool TestMode { get; set; }
}
