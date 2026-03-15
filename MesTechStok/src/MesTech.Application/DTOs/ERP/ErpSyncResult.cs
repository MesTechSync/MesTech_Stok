namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP sync operation result — returned by all ERP sync methods.
/// Success: whether the record was accepted by the ERP.
/// ErpRef: the ERP-side record ID on success.
/// ErrorMessage: error detail on failure.
/// Dalga 9: ilk tanimlandi. Dalga 11: factory method'lar eklendi.
/// </summary>
public record ErpSyncResult(bool Success, string? ErpRef, string? ErrorMessage)
{
    /// <summary>Basarili sync sonucu olusturur.</summary>
    public static ErpSyncResult Ok(string erpRef)
        => new(true, erpRef, null);

    /// <summary>Basarisiz sync sonucu olusturur.</summary>
    public static ErpSyncResult Fail(string errorMessage)
        => new(false, null, errorMessage);
}
