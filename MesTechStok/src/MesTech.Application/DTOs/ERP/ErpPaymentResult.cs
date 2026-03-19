namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP odeme islem sonucu.
/// </summary>
public record ErpPaymentResult(
    bool Success,
    string? Reference,
    string? ErrorMessage
)
{
    public static ErpPaymentResult Ok(string reference)
        => new(true, reference, null);

    public static ErpPaymentResult Failed(string error)
        => new(false, null, error);
}
