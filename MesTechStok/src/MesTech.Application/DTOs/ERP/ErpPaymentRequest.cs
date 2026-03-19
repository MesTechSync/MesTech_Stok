namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP odeme kaydi istegi.
/// </summary>
public record ErpPaymentRequest(
    string AccountCode,
    decimal Amount,
    string PaymentType,
    DateTime? DueDate,
    string? Description
);
