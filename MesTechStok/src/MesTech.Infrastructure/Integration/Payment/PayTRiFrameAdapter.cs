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
/// PayTR iFrame mode adapter.
/// Token generation → iFrame embed URL — transaction flow delegated to iFrame.
/// ProcessPayment returns a redirect/embed URL; the payment is completed in the browser.
/// Callback: PayTR POSTs result to merchant_ok_url / merchant_fail_url with hash verification.
/// Config keys: PayTR:MerchantId, PayTR:MerchantKey, PayTR:MerchantSalt,
///              PayTR:IFrameBaseUrl, PayTR:TestMode
/// </summary>
public class PayTRiFrameAdapter : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayTRiFrameAdapter> _logger;
    private readonly PayTRiFrameOptions _options;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string DefaultBaseUrl = "https://www.paytr.com";
    private const string TokenEndpoint = "/odeme/api/get-token";
    private const string StatusEndpoint = "/odeme/durum";
    private const string RefundEndpoint = "/odeme/iade";
    private const string BinEndpoint = "/odeme/taksit";
    private const string IFrameEmbedPath = "/odeme/iframe";

    public PaymentProviderType Provider => PaymentProviderType.PayTRiFrame;

    public PayTRiFrameAdapter(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<PayTRiFrameAdapter> logger)
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

    /// <summary>
    /// Generates iFrame token and returns embed URL.
    /// The browser must load <see cref="PaymentResult.RedirectUrl"/> in an iframe or redirect.
    /// </summary>
    public async Task<PaymentResult> ProcessPaymentAsync(
        PaymentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "[PayTRiFrame] ProcessPayment order={OrderId} amount={Amount}",
            request.OrderId, request.Amount);

        try
        {
            var merchantOid = request.OrderId.ToString("N");
            var paymentAmount = (long)(request.Amount * 100);
            var basketJson = BuildBasketJson(request.BasketItems);

            // iFrame token hash: HMAC-SHA256(merchant_id + user_ip + merchant_oid +
            //   email + payment_amount + user_basket + no_installment + max_installment +
            //   currency + test_mode + merchant_salt)
            var hashInput = string.Concat(
                _options.MerchantId,
                request.CustomerIp,
                merchantOid,
                "customer@mestech.app",
                paymentAmount.ToString(),
                basketJson,
                "0",    // no_installment
                "0",    // max_installment
                request.Currency,
                _options.TestMode ? "1" : "0",
                _options.MerchantSalt);

            var token = ComputeHmacSha256Base64(hashInput, _options.MerchantKey);

            var payload = new Dictionary<string, string>
            {
                ["merchant_id"] = _options.MerchantId,
                ["user_ip"] = request.CustomerIp,
                ["merchant_oid"] = merchantOid,
                ["email"] = "customer@mestech.app",
                ["payment_amount"] = paymentAmount.ToString(),
                ["paytr_token"] = token,
                ["user_basket"] = basketJson,
                ["debug_on"] = _options.TestMode ? "1" : "0",
                ["no_installment"] = "0",
                ["max_installment"] = "0",
                ["currency"] = request.Currency,
                ["test_mode"] = _options.TestMode ? "1" : "0",
                ["merchant_ok_url"] = request.ReturnUrl,
                ["merchant_fail_url"] = request.ReturnUrl
            };

            var response = await ExecuteWithRetryAsync(
                () => BuildFormRequest(TokenEndpoint, payload), ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("[PayTRiFrame] Token request failed {Status}: {Body}", response.StatusCode, body);
                return new PaymentResult(false, null, null, $"PayTR iFrame hatasi: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PayTRTokenResponse>(_jsonOptions, ct);

            if (result?.Status != "success")
            {
                _logger.LogWarning("[PayTRiFrame] Token failed: {Reason}", result?.Reason);
                return new PaymentResult(false, null, null, result?.Reason ?? "PayTR iFrame token hatasi");
            }

            // iFrame embed URL — merchant embeds this in <iframe src="...">
            var iFrameUrl = $"{_options.BaseUrl}{IFrameEmbedPath}/{result.Token}";

            _logger.LogInformation("[PayTRiFrame] iFrame URL generated for order {OrderId}", request.OrderId);
            return new PaymentResult(true, merchantOid, iFrameUrl, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayTRiFrame] ProcessPayment failed for order {OrderId}", request.OrderId);
            return new PaymentResult(false, null, null, ex.Message);
        }
    }

    public async Task<PaymentStatusResult> GetTransactionStatusAsync(
        string transactionId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        _logger.LogInformation("[PayTRiFrame] GetTransactionStatus merchantOid={Oid}", transactionId);

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
                () => BuildFormRequest(StatusEndpoint, payload), ct);

            if (!response.IsSuccessStatusCode)
                return new PaymentStatusResult(transactionId, PaymentTransactionStatus.Failed, 0m, null);

            var result = await response.Content.ReadFromJsonAsync<PayTRStatusResponse>(_jsonOptions, ct);

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
            _logger.LogError(ex, "[PayTRiFrame] GetTransactionStatus failed for {Oid}", transactionId);
            return new PaymentStatusResult(transactionId, PaymentTransactionStatus.Failed, 0m, null);
        }
    }

    public async Task<InstallmentOptions> GetInstallmentOptionsAsync(
        decimal amount,
        string? binNumber,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[PayTRiFrame] GetInstallmentOptions amount={Amount} bin={Bin}", amount, binNumber);

        try
        {
            var hashInput = $"{_options.MerchantId}{binNumber}{(long)(amount * 100)}{_options.MerchantSalt}";
            var token = ComputeHmacSha256Base64(hashInput, _options.MerchantKey);

            var payload = new Dictionary<string, string>
            {
                ["merchant_id"] = _options.MerchantId,
                ["card_type"] = binNumber?.Length >= 6 ? binNumber[..6] : string.Empty,
                ["amount"] = ((long)(amount * 100)).ToString(),
                ["paytr_token"] = token
            };

            var response = await ExecuteWithRetryAsync(
                () => BuildFormRequest(BinEndpoint, payload), ct);

            if (!response.IsSuccessStatusCode)
                return new InstallmentOptions(Array.Empty<InstallmentOption>());

            var result = await response.Content.ReadFromJsonAsync<PayTRBinResponse>(_jsonOptions, ct);

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
            _logger.LogError(ex, "[PayTRiFrame] GetInstallmentOptions failed");
            return new InstallmentOptions(Array.Empty<InstallmentOption>());
        }
    }

    public async Task<RefundResult> RefundAsync(
        string transactionId,
        decimal amount,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        _logger.LogInformation("[PayTRiFrame] Refund merchantOid={Oid} amount={Amount}", transactionId, amount);

        try
        {
            var refundAmount = (long)(amount * 100);
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
                () => BuildFormRequest(RefundEndpoint, payload), ct);

            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);

            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;

            if (status == "success")
            {
                _logger.LogInformation("[PayTRiFrame] Refund successful for {Oid}", transactionId);
                return new RefundResult(true, $"refund-{transactionId}", null);
            }

            var reason = doc.RootElement.TryGetProperty("err_no", out var e) ? e.GetString() : "Bilinmeyen hata";
            _logger.LogWarning("[PayTRiFrame] Refund failed for {Oid}: {Reason}", transactionId, reason);
            return new RefundResult(false, null, reason);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PayTRiFrame] Refund failed for {Oid}", transactionId);
            return new RefundResult(false, null, ex.Message);
        }
    }

    /// <summary>
    /// Verifies a PayTR callback POST hash.
    /// Expected POST fields: merchant_oid, status, total_amount, hash
    /// Hash validation: SHA256(merchant_oid + merchant_salt + status) + base64 == hash
    /// </summary>
    public bool VerifyCallback(string merchantOid, string status, string totalAmount, string receivedHash)
    {
        var raw = $"{merchantOid}{_options.MerchantSalt}{status}";
        var expected = ComputeHmacSha256Base64(raw, _options.MerchantKey);
        var isValid = string.Equals(expected, receivedHash, StringComparison.Ordinal);

        if (!isValid)
            _logger.LogWarning("[PayTRiFrame] Callback hash mismatch for merchantOid={Oid}", merchantOid);

        return isValid;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

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
        return new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(payload)
        };
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
            _logger.LogWarning(ex, "[PayTRiFrame] Circuit breaker open — returning 503");
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
                    _logger.LogWarning("[PayTRiFrame] Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[PayTRiFrame] Circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    private static PayTRiFrameOptions BindOptions(IConfiguration configuration)
    {
        return new PayTRiFrameOptions
        {
            MerchantId = configuration["PayTR:MerchantId"] ?? string.Empty,
            MerchantKey = configuration["PayTR:MerchantKey"] ?? string.Empty,
            MerchantSalt = configuration["PayTR:MerchantSalt"] ?? string.Empty,
            BaseUrl = configuration["PayTR:IFrameBaseUrl"] ?? DefaultBaseUrl,
            TestMode = bool.TryParse(configuration["PayTR:TestMode"], out var tm) && tm
        };
    }

    // ── Response models ───────────────────────────────────────────────────────

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
/// Strongly-typed PayTR iFrame configuration — bound from IConfiguration.
/// </summary>
internal sealed class PayTRiFrameOptions
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string MerchantSalt { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool TestMode { get; set; }
}
