using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Kargo etiketi yazdirma servisi — PDF/ZPL/PNG format destegi.
/// PDF: QuestPDF ile 10x15cm termal etiket (gercek PDF).
/// ZPL: Zebra/Argox termal yazici formati.
/// PNG: UTF-8 metin tabanli placeholder (SkiaSharp eklenebilir).
/// S06b — DEV 6.
/// </summary>
public class LabelPrintService : ILabelPrintService
{
    private readonly ILogger<LabelPrintService> _logger;

    public LabelPrintService(ILogger<LabelPrintService> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public Task<byte[]> GeneratePdfLabelAsync(ShipmentLabelDto label, CancellationToken ct = default)
    {
        _logger.LogInformation("PDF etiket olusturuluyor: {TrackingNumber}", label.TrackingNumber);

        return Task.Run(() =>
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigureLabelPage(page);
                    page.Content().Element(c => ComposeLabelContent(c, label));
                });
            });
            return document.GeneratePdf();
        }, ct);
    }

    /// <inheritdoc />
    public Task<string> GenerateZplLabelAsync(ShipmentLabelDto label, CancellationToken ct = default)
    {
        _logger.LogInformation("ZPL etiket olusturuluyor: {TrackingNumber}", label.TrackingNumber);

        var zpl = new StringBuilder();
        zpl.AppendLine("^XA");
        zpl.AppendLine("^CF0,30");
        zpl.AppendLine($"^FO50,50^FD{label.CargoProviderName}^FS");
        zpl.AppendLine($"^FO50,100^BY3^BCN,100,Y,N,N^FD{label.BarcodeData}^FS");
        zpl.AppendLine($"^FO50,230^FDGonderici: {label.SenderName}^FS");
        zpl.AppendLine($"^FO50,270^FDAlici: {label.RecipientName}^FS");
        zpl.AppendLine($"^FO50,310^FD{label.RecipientAddress}^FS");
        zpl.AppendLine($"^FO50,350^FD{label.RecipientCity} / {label.RecipientDistrict}^FS");
        zpl.AppendLine($"^FO50,400^FDTel: {label.RecipientPhone}^FS");
        zpl.AppendLine($"^FO50,450^FDKoli: {label.CurrentParcel}/{label.ParcelCount} | Agirlik: {label.Weight:N1} kg^FS");
        zpl.AppendLine($"^FO50,490^FDOdeme: {label.PaymentType}^FS");
        if (label.CodAmount.HasValue)
            zpl.AppendLine($"^FO50,530^FDTahsilat: {label.CodAmount:N2} TL^FS");
        zpl.AppendLine("^XZ");

        return Task.FromResult(zpl.ToString());
    }

    /// <inheritdoc />
    public Task<byte[]> GeneratePngLabelAsync(ShipmentLabelDto label, CancellationToken ct = default)
    {
        _logger.LogInformation("PNG etiket olusturuluyor: {TrackingNumber}", label.TrackingNumber);

        // Placeholder PNG — gercek implementasyon SkiaSharp ile yapilacak
        var content = BuildLabelText(label);
        var pngBytes = Encoding.UTF8.GetBytes(content);
        return Task.FromResult(pngBytes);
    }

    /// <inheritdoc />
    public Task<byte[]> GenerateBulkPdfLabelsAsync(IReadOnlyList<ShipmentLabelDto> labels, CancellationToken ct = default)
    {
        _logger.LogInformation("Toplu PDF etiket olusturuluyor: {Count} adet", labels.Count);

        return Task.Run(() =>
        {
            var document = Document.Create(container =>
            {
                foreach (var label in labels)
                {
                    ct.ThrowIfCancellationRequested();
                    container.Page(page =>
                    {
                        ConfigureLabelPage(page);
                        page.Content().Element(c => ComposeLabelContent(c, label));
                    });
                }
            });
            return document.GeneratePdf();
        }, ct);
    }

    // ── QuestPDF Label Composition ──

    /// <summary>
    /// 10cm x 15cm termal etiket boyutu yapilandirmasi.
    /// </summary>
    private static void ConfigureLabelPage(PageDescriptor page)
    {
        // 10cm x 15cm = standart termal kargo etiketi
        page.Size(new PageSize(10, 15, Unit.Centimetre));
        page.Margin(5, Unit.Millimetre);
        page.DefaultTextStyle(x => x.FontSize(9));
    }

    /// <summary>
    /// Etiket icerigini QuestPDF ile olusturur:
    /// Kargo firma, Gonderici, Alici, Takip No (buyuk), Barkod, Agirlik, Tahsilat.
    /// </summary>
    private static void ComposeLabelContent(IContainer container, ShipmentLabelDto label)
    {
        container.Column(col =>
        {
            // ── Kargo firma baslik ──
            col.Item().Background(Colors.Black).Padding(6).AlignCenter()
                .Text(label.CargoProviderName.ToUpperInvariant())
                .Bold().FontSize(14).FontColor(Colors.White);

            col.Item().PaddingTop(4);

            // ── Gonderici ──
            col.Item().Background(Colors.Grey.Lighten4).Padding(4).Column(sender =>
            {
                sender.Item().Text("GONDERICI").Bold().FontSize(7).FontColor(Colors.Grey.Darken2);
                sender.Item().Text(label.SenderName).FontSize(8);
                if (!string.IsNullOrWhiteSpace(label.SenderAddress))
                    sender.Item().Text(label.SenderAddress).FontSize(7);
                if (!string.IsNullOrWhiteSpace(label.SenderPhone))
                    sender.Item().Text($"Tel: {label.SenderPhone}").FontSize(7);
            });

            col.Item().PaddingTop(3);

            // ── Alici ──
            col.Item().Border(1).Padding(5).Column(recipient =>
            {
                recipient.Item().Text("ALICI").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                recipient.Item().Text(label.RecipientName).Bold().FontSize(11);
                recipient.Item().Text(label.RecipientAddress).FontSize(9);
                recipient.Item().Text($"{label.RecipientCity} / {label.RecipientDistrict}").FontSize(9);
                recipient.Item().Text($"Tel: {label.RecipientPhone}").FontSize(9);
            });

            col.Item().PaddingTop(5);

            // ── Takip Numarasi (buyuk, bold) ──
            col.Item().AlignCenter().Text(label.TrackingNumber)
                .Bold().FontSize(18);

            col.Item().PaddingTop(2);

            // ── Barkod placeholder (metin olarak) ──
            col.Item().AlignCenter().Border(1).Padding(4)
                .Text($"|||  {label.BarcodeData}  |||")
                .FontSize(12).FontFamily("Courier New");

            col.Item().PaddingTop(4);

            // ── Alt bilgiler: Agirlik, Koli, Odeme ──
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text($"Koli: {label.CurrentParcel}/{label.ParcelCount}").FontSize(8);
                    left.Item().Text($"Agirlik: {label.Weight:N1} kg").FontSize(8);
                    left.Item().Text($"Siparis: {label.OrderNumber}").FontSize(7);
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().Text($"Odeme: {label.PaymentType}").FontSize(8);
                    if (label.CodAmount.HasValue)
                        right.Item().Text($"Tahsilat: {label.CodAmount:N2} TL")
                            .Bold().FontSize(10).FontColor(Colors.Red.Medium);
                    right.Item().Text($"Tarih: {label.ShipDate:dd.MM.yyyy}").FontSize(7);
                });
            });
        });
    }

    /// <summary>
    /// ZPL ve PNG fallback icin metin tabanli etiket icerigi.
    /// </summary>
    private static string BuildLabelText(ShipmentLabelDto label)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===================================");
        sb.AppendLine($"  {label.CargoProviderName}");
        sb.AppendLine("===================================");
        sb.AppendLine($"  Takip No: {label.TrackingNumber}");
        sb.AppendLine($"  Barkod: {label.BarcodeData}");
        sb.AppendLine();
        sb.AppendLine($"  Gonderici: {label.SenderName}");
        sb.AppendLine($"  {label.SenderAddress}");
        sb.AppendLine();
        sb.AppendLine($"  Alici: {label.RecipientName}");
        sb.AppendLine($"  {label.RecipientAddress}");
        sb.AppendLine($"  {label.RecipientCity} / {label.RecipientDistrict}");
        sb.AppendLine($"  Tel: {label.RecipientPhone}");
        sb.AppendLine();
        sb.AppendLine($"  Koli: {label.CurrentParcel}/{label.ParcelCount}");
        sb.AppendLine($"  Agirlik: {label.Weight:N1} kg");
        sb.AppendLine($"  Odeme: {label.PaymentType}");
        if (label.CodAmount.HasValue)
            sb.AppendLine($"  Tahsilat: {label.CodAmount:N2} TL");
        sb.AppendLine($"  Siparis: {label.OrderNumber}");
        sb.AppendLine($"  Tarih: {label.ShipDate:dd.MM.yyyy}");
        sb.AppendLine("===================================");
        return sb.ToString();
    }
}
