namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Ba/Bs formu XML export servisi.
/// GiB'e gonderilecek Ba/Bs bildirim XML'ini uretir.
/// VUK 396 Sira No'lu Genel Teblig formatina uygun.
/// </summary>
public interface IBaBsXmlExportService
{
    /// <summary>
    /// BaBsReportDto verisini GiB Ba/Bs XML formatina donusturur.
    /// </summary>
    /// <param name="report">Rapor verisi (BaBsReportService'ten uretilmis).</param>
    /// <param name="formType">Form tipi: "Ba" (alis) veya "Bs" (satis).</param>
    /// <param name="year">Donem yili.</param>
    /// <param name="month">Donem ayi (1-12).</param>
    /// <param name="tenantVKN">Mukellef VKN (10 veya 11 hane).</param>
    /// <param name="tenantName">Mukellef unvani.</param>
    /// <returns>UTF-8 encoded XML byte array.</returns>
    Task<byte[]> ExportToXmlAsync(
        BaBsReportDto report,
        string formType,
        int year,
        int month,
        string tenantVKN,
        string tenantName);
}
