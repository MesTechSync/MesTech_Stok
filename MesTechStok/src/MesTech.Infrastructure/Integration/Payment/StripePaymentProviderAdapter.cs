using System.Text.Json;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppPayment = MesTech.Application.Interfaces;
using DomainPayment = MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Integration.Payment;

/// <summary>
/// Bridges <see cref="DomainPayment.IPaymentGateway"/> (Domain) to <see cref="AppPayment.IPaymentProvider"/> (Application).
/// Wraps StripePaymentGateway so that PaymentEndpoints can resolve Stripe via IPaymentProvider.
/// </summary>
public sealed class StripePaymentProviderAdapter : AppPayment.IPaymentProvider
{
    private readonly StripePaymentGateway _gateway;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly StripeOptions _options;
    private readonly ILogger<StripePaymentProviderAdapter> _logger;

    public PaymentProviderType Provider => PaymentProviderType.Stripe;

    public StripePaymentProviderAdapter(
        StripePaymentGateway gateway,
        IHttpClientFactory httpClientFactory,
        IOptions<StripeOptions> options,
        ILogger<StripePaymentProviderAdapter> logger)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AppPayment.PaymentResult> ProcessPaymentAsync(AppPayment.PaymentRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Stripe ProcessPayment: {OrderId} {Amount} {Currency}",
            request.OrderId, request.Amount, request.Currency);

        var gatewayResult = await _gateway.ChargeAsync(
            request.Amount,
            request.Currency,
            request.CardToken ?? string.Empty,
            $"Order {request.OrderId}",
            ct).ConfigureAwait(false);

        return new AppPayment.PaymentResult(
            Success: !string.IsNullOrEmpty(gatewayResult.TransactionId),
            TransactionId: gatewayResult.TransactionId,
            RedirectUrl: null,
            ErrorMessage: gatewayResult.ErrorMessage);
    }

    public async Task<AppPayment.PaymentStatusResult> GetTransactionStatusAsync(string transactionId, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning("Stripe GetTransactionStatus: {TransactionId} — API key not configured, returning Pending", transactionId);
            return new AppPayment.PaymentStatusResult(transactionId, PaymentTransactionStatus.Pending, 0m, null);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Stripe");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.SecretKey);

            using var response = await client.GetAsync(
                $"{_options.BaseUrl}/v1/payment_intents/{transactionId}", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Stripe status query failed: {StatusCode} for {TransactionId}",
                    response.StatusCode, transactionId);
                return new AppPayment.PaymentStatusResult(transactionId, PaymentTransactionStatus.Failed, 0m, null);
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var s) ? s.GetString() : "unknown";
            var amount = root.TryGetProperty("amount", out var a) ? a.GetInt64() / 100m : 0m;
            var created = root.TryGetProperty("created", out var c)
                ? DateTimeOffset.FromUnixTimeSeconds(c.GetInt64()).UtcDateTime
                : (DateTime?)null;

            var mappedStatus = status switch
            {
                "succeeded" => PaymentTransactionStatus.Completed,
                "canceled" => PaymentTransactionStatus.Failed,
                "requires_payment_method" or "requires_confirmation" or "requires_action" or "processing"
                    => PaymentTransactionStatus.Pending,
                _ => PaymentTransactionStatus.Failed
            };

            _logger.LogInformation("Stripe status: {TransactionId} → {Status} ({StripeStatus})",
                transactionId, mappedStatus, status);

            return new AppPayment.PaymentStatusResult(
                transactionId, mappedStatus, amount,
                mappedStatus == PaymentTransactionStatus.Completed ? created : null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Stripe GetTransactionStatus failed for {TransactionId}", transactionId);
            return new AppPayment.PaymentStatusResult(transactionId, PaymentTransactionStatus.Pending, 0m, null);
        }
    }

    public Task<AppPayment.InstallmentOptions> GetInstallmentOptionsAsync(decimal amount, string? binNumber, CancellationToken ct = default)
    {
        _logger.LogDebug("Stripe does not support installment options — returning empty");
        return Task.FromResult(new AppPayment.InstallmentOptions(Array.Empty<AppPayment.InstallmentOption>()));
    }

    public async Task<AppPayment.RefundResult> RefundAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        _logger.LogInformation("Stripe Refund: {TransactionId} {Amount}", transactionId, amount);

        var gatewayResult = await _gateway.RefundAsync(transactionId, amount, ct).ConfigureAwait(false);

        return new AppPayment.RefundResult(
            Success: !string.IsNullOrEmpty(gatewayResult.TransactionId),
            RefundId: gatewayResult.TransactionId,
            ErrorMessage: gatewayResult.ErrorMessage);
    }
}
