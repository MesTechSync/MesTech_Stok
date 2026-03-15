namespace MesTech.Application.DTOs.Fulfillment;

/// <summary>
/// Inbound sevkiyat olusturma sonucu.
/// </summary>
public record InboundResult(
    bool Success,
    string ShipmentId,
    string? ErrorMessage = null,
    DateTime? EstimatedReceiveDate = null
);
