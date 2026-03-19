using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// QuestPDF ile fatura/irsaliye/SMM PDF üretimi.
/// Türkiye e-Fatura standardına uygun A4 şablon.
/// </summary>
public class QuestPdfInvoiceGenerator : IInvoicePdfGenerator
{
    private readonly ILogger<QuestPdfInvoiceGenerator> _logger;

    public QuestPdfInvoiceGenerator(ILogger<QuestPdfInvoiceGenerator> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateInvoicePdfAsync(InvoicePdfRequest req, CancellationToken ct = default)
    {
        return Task.Run(() => GeneratePdf(req, "FATURA", includeVat: true, includeWithholding: false), ct);
    }

    public Task<byte[]> GenerateWaybillPdfAsync(InvoicePdfRequest req, CancellationToken ct = default)
    {
        return Task.Run(() => GeneratePdf(req, "İRSALİYE", includeVat: false, includeWithholding: false), ct);
    }

    public Task<byte[]> GenerateESMMPdfAsync(InvoicePdfRequest req, CancellationToken ct = default)
    {
        return Task.Run(() => GeneratePdf(req, "SERBEST MESLEK MAKBUZU", includeVat: true, includeWithholding: true), ct);
    }

    public Task<byte[]> GenerateBulkPdfAsync(List<InvoicePdfRequest> requests, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var document = Document.Create(container =>
            {
                foreach (var req in requests)
                {
                    container.Page(page =>
                    {
                        ConfigurePage(page);
                        page.Header().Element(c => ComposeHeader(c, req, "FATURA"));
                        page.Content().Element(c => ComposeContent(c, req, true, false));
                        page.Footer().Element(c => ComposeFooter(c, req));
                    });
                }
            });
            return document.GeneratePdf();
        }, ct);
    }

    private byte[] GeneratePdf(InvoicePdfRequest req, string title, bool includeVat, bool includeWithholding)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Header().Element(c => ComposeHeader(c, req, title));
                page.Content().Element(c => ComposeContent(c, req, includeVat, includeWithholding));
                page.Footer().Element(c => ComposeFooter(c, req));
            });
        });

        _logger.LogDebug("PDF generated: {InvoiceNumber} ({Title})", req.InvoiceNumber, title);
        return document.GeneratePdf();
    }

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(1.5f, Unit.Centimetre);
        page.DefaultTextStyle(x => x.FontSize(9));
    }

    private static void ComposeHeader(IContainer container, InvoicePdfRequest req, string title)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("MesTech").Bold().FontSize(16);
                col.Item().Text(req.SellerName).FontSize(10);
                col.Item().Text($"VKN: {req.SellerVkn}");
                col.Item().Text($"Vergi Dairesi: {req.SellerTaxOffice}");
                col.Item().Text(req.SellerAddress);
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text(title).Bold().FontSize(14);
                col.Item().Text($"No: {req.InvoiceNumber}").FontSize(10);
                col.Item().Text($"Tarih: {req.InvoiceDate:dd.MM.yyyy}");
                col.Item().Text($"Tip: {req.InvoiceType}");
            });
        });
    }

    private static void ComposeContent(IContainer container, InvoicePdfRequest req, bool includeVat, bool includeWithholding)
    {
        container.PaddingVertical(10).Column(col =>
        {
            // Alıcı bilgileri
            col.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(buyer =>
            {
                buyer.Item().Text("ALICI BİLGİLERİ").Bold().FontSize(9);
                buyer.Item().Text($"Ad/Ünvan: {req.BuyerName}");
                if (!string.IsNullOrEmpty(req.BuyerVkn))
                    buyer.Item().Text($"VKN/TCKN: {req.BuyerVkn}");
                if (!string.IsNullOrEmpty(req.BuyerTaxOffice))
                    buyer.Item().Text($"Vergi Dairesi: {req.BuyerTaxOffice}");
                buyer.Item().Text($"Adres: {req.BuyerAddress}");
            });

            // e-İrsaliye ek bilgiler
            if (!string.IsNullOrEmpty(req.DriverName))
            {
                col.Item().PaddingTop(5).Background(Colors.Blue.Lighten5).Padding(8).Column(ship =>
                {
                    ship.Item().Text("SEVK BİLGİLERİ").Bold().FontSize(9);
                    ship.Item().Text($"Sürücü: {req.DriverName}");
                    if (!string.IsNullOrEmpty(req.VehiclePlate))
                        ship.Item().Text($"Araç Plakası: {req.VehiclePlate}");
                    if (req.ShipmentDate.HasValue)
                        ship.Item().Text($"Sevk Tarihi: {req.ShipmentDate:dd.MM.yyyy}");
                    if (!string.IsNullOrEmpty(req.ShipmentAddress))
                        ship.Item().Text($"Sevk Adresi: {req.ShipmentAddress}");
                });
            }

            // e-SMM ek bilgiler
            if (!string.IsNullOrEmpty(req.ProfessionalTitle))
            {
                col.Item().PaddingTop(5).Background(Colors.Green.Lighten5).Padding(8).Column(smm =>
                {
                    smm.Item().Text("MESLEK BİLGİLERİ").Bold().FontSize(9);
                    smm.Item().Text($"Meslek: {req.ProfessionalTitle}");
                    if (!string.IsNullOrEmpty(req.ActivityCode))
                        smm.Item().Text($"Faaliyet Kodu (NACE): {req.ActivityCode}");
                });
            }

            // Kalemler tablosu
            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    if (includeVat) columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // Başlık
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("#").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Ürün/Hizmet").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Miktar").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Birim").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Fiyat").Bold();
                    if (includeVat)
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("KDV").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Toplam").Bold();
                });

                foreach (var line in req.Lines)
                {
                    table.Cell().Padding(3).Text(line.LineNumber.ToString());
                    table.Cell().Padding(3).Text(line.Description);
                    table.Cell().Padding(3).AlignRight().Text(line.Quantity.ToString("N2"));
                    table.Cell().Padding(3).Text(line.Unit);
                    table.Cell().Padding(3).AlignRight().Text($"{line.UnitPrice:N2}");
                    if (includeVat)
                        table.Cell().Padding(3).AlignRight().Text($"%{line.VatRate:N0}");
                    table.Cell().Padding(3).AlignRight().Text($"{line.LineTotal:N2}");
                }
            });

            // Toplamlar
            col.Item().PaddingTop(10).AlignRight().Column(totals =>
            {
                totals.Item().Text($"Ara Toplam: {req.SubTotal:N2} {req.Currency}");
                if (includeVat)
                    totals.Item().Text($"KDV Toplam: {req.TaxTotal:N2} {req.Currency}");
                if (includeWithholding && req.WithholdingRate.HasValue)
                {
                    totals.Item().Text($"Stopaj (%{req.WithholdingRate * 100:N0}): -{req.WithholdingAmount:N2} {req.Currency}");
                    totals.Item().Text($"Net Ödeme: {req.GrandTotal - (req.WithholdingAmount ?? 0):N2} {req.Currency}").Bold();
                }
                totals.Item().PaddingTop(5).Text($"GENEL TOPLAM: {req.GrandTotal:N2} {req.Currency}").Bold().FontSize(12);
            });
        });
    }

    private static void ComposeFooter(IContainer container, InvoicePdfRequest req)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                if (!string.IsNullOrEmpty(req.GibUuid))
                    col.Item().Text($"ETTN: {req.GibUuid}").FontSize(7);
                col.Item().Text("MesTech Entegratör Yazılımı").FontSize(7);
            });

            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Sayfa ").FontSize(7);
                text.CurrentPageNumber().FontSize(7);
                text.Span("/").FontSize(7);
                text.TotalPages().FontSize(7);
            });
        });
    }
}
