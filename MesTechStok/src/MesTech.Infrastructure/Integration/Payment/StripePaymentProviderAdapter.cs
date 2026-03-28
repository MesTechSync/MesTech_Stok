using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<StripePaymentProviderAdapter> _logger;

    public PaymentProviderType Provider => PaymentProviderType.Stripe;

    public StripePaymentProviderAdapter(
        StripePaymentGateway gateway,
        ILogger<StripePaymentProviderAdapter> logger)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
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

    public Task<AppPayment.PaymentStatusResult> GetTransactionStatusAsync(string transactionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Stripe GetTransactionStatus: {TransactionId}", transactionId);

        return Task.FromResult(new AppPayment.PaymentStatusResult(
            TransactionId: transactionId,
            Status: PaymentTransactionStatus.Completed,
            Amount: 0m,
            PaidAt: DateTime.UtcNow));
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
