using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;

namespace MesTechStok.Desktop.Services
{
    public class PdfReportService
    {
        private class HeaderFooterPageEvent : PdfPageEventHelper
        {
            private readonly string _companyName;
            private readonly string _reportTitle;
            private BaseFont? _baseFont;

            public HeaderFooterPageEvent(string companyName, string reportTitle)
            {
                _companyName = companyName;
                _reportTitle = reportTitle;
            }

            public override void OnOpenDocument(PdfWriter writer, Document document)
            {
                try
                {
                    // Try Segoe UI else Helvetica
                    var segoePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
                    _baseFont = BaseFont.CreateFont(segoePath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                }
                catch
                {
                    _baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.NOT_EMBEDDED);
                }
            }

            public override void OnEndPage(PdfWriter writer, Document document)
            {
                if (_baseFont == null) return;

                var cb = writer.DirectContent;
                var pageSize = document.PageSize;
                var margin = 36f;

                // Header: Company – Report Title – Date
                var headerText = $"{_companyName} – {_reportTitle}";
                var dateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                cb.BeginText();
                cb.SetFontAndSize(_baseFont, 9);
                cb.ShowTextAligned(Element.ALIGN_LEFT, headerText, pageSize.GetLeft(margin), pageSize.GetTop(margin - 6), 0);
                cb.ShowTextAligned(Element.ALIGN_RIGHT, dateText, pageSize.GetRight(margin), pageSize.GetTop(margin - 6), 0);
                cb.EndText();

                // Header separator line
                cb.SetLineWidth(0.5f);
                cb.SetGrayStroke(0.6f);
                cb.MoveTo(pageSize.GetLeft(margin), pageSize.GetTop(margin - 10));
                cb.LineTo(pageSize.GetRight(margin), pageSize.GetTop(margin - 10));
                cb.Stroke();

                // Footer: Page number
                var footerText = $"Sayfa {writer.PageNumber}";
                cb.BeginText();
                cb.SetFontAndSize(_baseFont, 9);
                cb.ShowTextAligned(Element.ALIGN_CENTER, footerText, (pageSize.Left + pageSize.Right) / 2, pageSize.GetBottom(margin - 12), 0);
                cb.EndText();
            }
        }

        public async Task ExportLowStockReportAsync(string filePath, string companyName, IList<(string Name, int Stock)> lowStockItems)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Dosya yolu geçersiz", nameof(filePath));
            lowStockItems = lowStockItems ?? Array.Empty<(string, int)>();

            await Task.Run(() =>
            {
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

                using var document = new Document(PageSize.A4, 36, 36, 64, 48);
                var writer = PdfWriter.GetInstance(document, fs);
                writer.CloseStream = true;
                writer.SetFullCompression();
                writer.PageEvent = new HeaderFooterPageEvent(companyName, "Kritik Stok Raporu");

                document.AddAuthor(companyName);
                document.AddCreator("MesTech Stok Takip Sistemi");
                document.AddTitle("Kritik Stok Raporu");
                document.AddSubject("Minimum stok eşiğinin altında kalan ürünler");

                document.Open();

                // Font: Windows sisteminden Segoe UI kullan, yoksa Helvetica'ya düş
                Font titleFont, headerFont, cellFont;
                try
                {
                    var segoePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
                    BaseFont bf = BaseFont.CreateFont(segoePath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    titleFont = new Font(bf, 16, Font.BOLD, new BaseColor(0, 0, 0));
                    headerFont = new Font(bf, 11, Font.BOLD, new BaseColor(255, 255, 255));
                    cellFont = new Font(bf, 10, Font.NORMAL, new BaseColor(0, 0, 0));
                }
                catch
                {
                    BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.NOT_EMBEDDED);
                    titleFont = new Font(bf, 16, Font.BOLD, new BaseColor(0, 0, 0));
                    headerFont = new Font(bf, 11, Font.BOLD, new BaseColor(255, 255, 255));
                    cellFont = new Font(bf, 10, Font.NORMAL, new BaseColor(0, 0, 0));
                }

                // Başlık
                var title = new Paragraph($"{companyName} – Kritik Stok Raporu", titleFont) { Alignment = Element.ALIGN_LEFT };
                document.Add(title);
                var meta = new Paragraph($"Toplam Ürün: {lowStockItems.Count}", cellFont);
                meta.SpacingAfter = 8f;
                document.Add(meta);
                // Visual separator
                var line = new LineSeparator(0.5f, 100f, new BaseColor(224, 224, 224), Element.ALIGN_CENTER, -2);
                document.Add(new Chunk(line));
                document.Add(new Paragraph(" "));

                // Tablo
                var table = new PdfPTable(2)
                {
                    WidthPercentage = 100
                };
                table.SetWidths(new float[] { 4f, 1.2f });
                table.HeaderRows = 1;
                table.SpacingBefore = 6f;

                var headerBg = new BaseColor(0xFB, 0x8C, 0x00); // turuncu başlık
                table.AddCell(CreateHeaderCell("Ürün Adı", headerFont, headerBg));
                table.AddCell(CreateHeaderCell("Stok", headerFont, headerBg, Element.ALIGN_RIGHT));

                foreach (var item in lowStockItems.OrderBy(x => x.Stock).ThenBy(x => x.Name))
                {
                    table.AddCell(CreateBodyCell(item.Name, cellFont));
                    table.AddCell(CreateBodyCell(item.Stock.ToString(), cellFont, Element.ALIGN_RIGHT));
                }

                document.Add(table);
                document.Close();
                writer.Close();
            });
        }

        private static PdfPCell CreateHeaderCell(string text, Font font, BaseColor background, int hAlign = Element.ALIGN_LEFT)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = hAlign,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                BackgroundColor = background,
                PaddingTop = 6f,
                PaddingBottom = 6f,
                PaddingLeft = 8f,
                PaddingRight = 8f
            };
            return cell;
        }

        private static PdfPCell CreateBodyCell(string text, Font font, int hAlign = Element.ALIGN_LEFT)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = hAlign,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                PaddingTop = 5f,
                PaddingBottom = 5f,
                PaddingLeft = 8f,
                PaddingRight = 8f
            };
            return cell;
        }
    }
}


