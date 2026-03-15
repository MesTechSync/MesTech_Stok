using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Odeme saglayici adapter'i — PayTR Direct ve iFrame modlari icin implement edilir.
/// </summary>
public interface IPaymentProvider
{
    PaymentProviderType Provider { get; }
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default);
    Task<PaymentStatusResult> GetTransactionStatusAsync(string transactionId, CancellationToken ct = default);
    Task<InstallmentOptions> GetInstallmentOptionsAsync(decimal amount, string? binNumber, CancellationToken ct = default);
    Task<RefundResult> RefundAsync(string transactionId, decimal amount, CancellationToken ct = default);
}

public record PaymentRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string? CardToken,
    string ReturnUrl,
    string CustomerIp,
    IReadOnlyList<BasketItem>? BasketItems = null);

public record BasketItem(
    string Id,
    string Name,
    string Category,
    decimal Price);

public record PaymentResult(
    bool Success,
    string? TransactionId,
    string? RedirectUrl,
    string? ErrorMessage);

public record PaymentStatusResult(
    string TransactionId,
    PaymentTransactionStatus Status,
    decimal Amount,
    DateTime? PaidAt);

public record InstallmentOptions(
    IReadOnlyList<InstallmentOption> Options);

public record InstallmentOption(
    int Count,
    decimal TotalAmount,
    decimal MonthlyAmount,
    decimal InterestRate);

public record RefundResult(
    bool Success,
    string? RefundId,
    string? ErrorMessage);
