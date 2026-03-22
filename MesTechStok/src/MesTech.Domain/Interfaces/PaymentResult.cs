namespace MesTech.Domain.Interfaces;

/// <summary>Odeme sonucu.</summary>
public record PaymentResult(
    bool Success,
    string? TransactionId,
    string? ErrorMessage = null,
    string? ErrorCode = null);
