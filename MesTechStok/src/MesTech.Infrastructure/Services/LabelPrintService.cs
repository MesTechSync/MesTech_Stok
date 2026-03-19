using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Kargo etiketi yazdırma servisi — PDF/ZPL/PNG format desteği.
/// PDF: basit metin tabanlı (QuestPDF eklenebilir)
/// ZPL: Zebra/Argox termal yazıcı formatı
/// PNG: UTF-8 metin tabanlı placeholder (SkiaSharp eklenebilir)
/// </summary>
public class LabelPrintService : ILabelPrintService
{
    private readonly ILogger<LabelPrintService> _logger;

    public LabelPrintService(ILogger<LabelPrintService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<byte[]> GeneratePdfLabelAsync(ShipmentLabelDto label, CancellationToken ct = default)
    {
        _logger.LogInformation("PDF etiket oluşturuluyor: {TrackingNumber}", label.TrackingNumber);

        // Placeholder PDF — gerçek implementasyon QuestPDF veya iTextSharp ile yapılacak
        var content = BuildLabelText(label);
        var pdfBytes = Encoding.UTF8.GetBytes($"%PDF-1.4\n% MesTech Kargo Etiketi\n{content}\n%%EOF");
        return Task.FromResult(pdfBytes);
    }

    /// <inheritdoc />
    public Task<string> GenerateZplLabelAsync(ShipmentLabelDto label, CancellationToken ct = default)
    {
        _logger.LogInformation("ZPL etiket oluşturuluyor: {TrackingNumber}", label.TrackingNumber);

        var zpl = new StringBuilder();
        zpl.AppendLine("^XA");
        zpl.AppendLine("^CF0,30");
        zpl.AppendLine($"^FO50,50^FD{label.CargoProviderName}^FS");
        zpl.AppendLine($"^FO50,100^BY3^BCN,100,Y,N,N^FD{label.BarcodeData}^FS");
        zpl.AppendLine($"^FO50,230^FDGönderici: {label.SenderName}^FS");
        zpl.AppendLine($"^FO50,270^FDAlıcı: {label.RecipientName}^FS");
        zpl.AppendLine($"^FO50,310^FD{label.RecipientAddress}^FS");
        zpl.AppendLine($"^FO50,350^FD{label.RecipientCity} / {label.RecipientDistrict}^FS");
        zpl.AppendLine($"^FO50,400^FDTel: {label.RecipientPhone}^FS");
        zpl.AppendLine($"^FO50,450^FDKoli: {label.CurrentParcel}/{label.ParcelCount} | Ağırlık: {label.Weight:N1} kg^FS");
        zpl.AppendLine($"^FO50,490^FDÖdeme: {label.PaymentType}^FS");
        if (label.CodAmount.HasValue)
            zpl.AppendLine($"^FO50,530^FDTahsilat: {label.CodAmount:N2} TL^FS");
        zpl.AppendLine("^XZ");

        return Task.FromResult(zpl.ToString());
    }

    /// <inheritdoc />
    public Task<byte[]> GeneratePngLabelAsync(ShipmentLabelDto label, CancellationToken ct = default)
    {
        _logger.LogInformation("PNG etiket oluşturuluyor: {TrackingNumber}", label.TrackingNumber);

        // Placeholder PNG — gerçek implementasyon SkiaSharp ile yapılacak
        var content = BuildLabelText(label);
        var pngBytes = Encoding.UTF8.GetBytes(content);
        return Task.FromResult(pngBytes);
    }

    /// <inheritdoc />
    public Task<byte[]> GenerateBulkPdfLabelsAsync(IReadOnlyList<ShipmentLabelDto> labels, CancellationToken ct = default)
    {
        _logger.LogInformation("Toplu PDF etiket oluşturuluyor: {Count} adet", labels.Count);

        var sb = new StringBuilder();
        sb.AppendLine("%PDF-1.4");
        sb.AppendLine("% MesTech Toplu Kargo Etiketi");
        for (int i = 0; i < labels.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine($"\n--- Sayfa {i + 1}/{labels.Count} ---");
            sb.AppendLine(BuildLabelText(labels[i]));
        }
        sb.AppendLine("%%EOF");

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static string BuildLabelText(ShipmentLabelDto label)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"═══════════════════════════════════");
        sb.AppendLine($"  {label.CargoProviderName}");
        sb.AppendLine($"═══════════════════════════════════");
        sb.AppendLine($"  Takip No: {label.TrackingNumber}");
        sb.AppendLine($"  Barkod: {label.BarcodeData}");
        sb.AppendLine();
        sb.AppendLine($"  Gönderici: {label.SenderName}");
        sb.AppendLine($"  {label.SenderAddress}");
        sb.AppendLine();
        sb.AppendLine($"  Alıcı: {label.RecipientName}");
        sb.AppendLine($"  {label.RecipientAddress}");
        sb.AppendLine($"  {label.RecipientCity} / {label.RecipientDistrict}");
        sb.AppendLine($"  Tel: {label.RecipientPhone}");
        sb.AppendLine();
        sb.AppendLine($"  Koli: {label.CurrentParcel}/{label.ParcelCount}");
        sb.AppendLine($"  Ağırlık: {label.Weight:N1} kg");
        sb.AppendLine($"  Ödeme: {label.PaymentType}");
        if (label.CodAmount.HasValue)
            sb.AppendLine($"  Tahsilat: {label.CodAmount:N2} TL");
        sb.AppendLine($"  Sipariş: {label.OrderNumber}");
        sb.AppendLine($"  Tarih: {label.ShipDate:dd.MM.yyyy}");
        sb.AppendLine($"═══════════════════════════════════");
        return sb.ToString();
    }
}
