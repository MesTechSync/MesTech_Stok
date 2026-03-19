namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP irsaliye islem sonucu.
/// </summary>
public record ErpWaybillResult(
    bool Success,
    string? WaybillNumber,
    DateTime? WaybillDate,
    string? ErrorMessage
)
{
    public static ErpWaybillResult Ok(string waybillNumber, DateTime waybillDate)
        => new(true, waybillNumber, waybillDate, null);

    public static ErpWaybillResult Failed(string error)
        => new(false, null, null, error);
}
