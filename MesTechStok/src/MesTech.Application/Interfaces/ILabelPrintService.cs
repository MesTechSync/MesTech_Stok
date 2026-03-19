using MesTech.Application.DTOs.Cargo;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Kargo etiketi yazdırma servisi — PDF, ZPL, PNG format desteği.
/// </summary>
public interface ILabelPrintService
{
    /// <summary>PDF formatında etiket oluşturur (A4 lazer yazıcı).</summary>
    Task<byte[]> GeneratePdfLabelAsync(ShipmentLabelDto label, CancellationToken ct = default);

    /// <summary>ZPL formatında etiket oluşturur (termal yazıcı — Zebra/Argox).</summary>
    Task<string> GenerateZplLabelAsync(ShipmentLabelDto label, CancellationToken ct = default);

    /// <summary>PNG formatında etiket oluşturur (ekrandan yazdır).</summary>
    Task<byte[]> GeneratePngLabelAsync(ShipmentLabelDto label, CancellationToken ct = default);

    /// <summary>Toplu PDF etiket oluşturur (N etiket → 1 PDF, N sayfa).</summary>
    Task<byte[]> GenerateBulkPdfLabelsAsync(IReadOnlyList<ShipmentLabelDto> labels, CancellationToken ct = default);
}
