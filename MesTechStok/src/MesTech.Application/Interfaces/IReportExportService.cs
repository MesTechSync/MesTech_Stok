namespace MesTech.Application.Interfaces;

/// <summary>
/// Generic rapor export servisi — herhangi bir DTO koleksiyonunu Excel veya CSV formatinda dondurur.
/// </summary>
public interface IReportExportService
{
    /// <summary>
    /// Veriyi Excel (.xlsx) formatinda export eder.
    /// </summary>
    /// <typeparam name="T">DTO tipi — public property'ler kolon olarak yazilir.</typeparam>
    /// <param name="data">Export edilecek veri koleksiyonu.</param>
    /// <param name="sheetName">Excel sayfa adi.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Excel dosyasi icerigi (byte array).</returns>
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName, CancellationToken ct = default);

    /// <summary>
    /// Veriyi CSV formatinda export eder (UTF-8 BOM, Excel uyumlu).
    /// </summary>
    /// <typeparam name="T">DTO tipi — public property'ler kolon olarak yazilir.</typeparam>
    /// <param name="data">Export edilecek veri koleksiyonu.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>CSV dosyasi icerigi (byte array).</returns>
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CancellationToken ct = default);

    /// <summary>
    /// Veriyi PDF formatinda export eder (QuestPDF ile tablo gorunumu).
    /// </summary>
    /// <typeparam name="T">DTO tipi — public property'ler kolon olarak yazilir.</typeparam>
    /// <param name="data">Export edilecek veri koleksiyonu.</param>
    /// <param name="title">PDF rapor basligi.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PDF dosyasi icerigi (byte array).</returns>
    Task<byte[]> ExportToPdfAsync<T>(IEnumerable<T> data, string title, CancellationToken ct = default);
}
