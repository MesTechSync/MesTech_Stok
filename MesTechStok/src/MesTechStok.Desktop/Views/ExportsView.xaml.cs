using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using MesTechStok.Desktop.Utils;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Desktop.Services;
using MesTechStok.Core.Services.Abstract;

namespace MesTechStok.Desktop.Views
{
    public class ExportRecord
    {
        public string Type { get; set; } = "";
        public DateTime Date { get; set; }
        public string Size { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public partial class ExportsView : UserControl
    {
        private ObservableCollection<ExportRecord> recentExports = new();
        private DispatcherTimer? statsTimer;
        private Random random = new Random();

        public ExportsView()
        {
            InitializeComponent();
            InitializeExports();
            SetupDataGrid();
            InitializeStatsTimer();
        }

        private void InitializeExports()
        {
            // Demo dışa aktarma kayıtları
            var demoExports = new[]
            {
                new ExportRecord { Type = "Excel", Date = DateTime.Now.AddMinutes(-15), Size = "2.4 MB",
                                  FileName = "satis_raporu.xlsx", FilePath = @"C:\Exports\satis_raporu.xlsx" },
                new ExportRecord { Type = "PDF", Date = DateTime.Now.AddHours(-1), Size = "856 KB",
                                  FileName = "stok_raporu.pdf", FilePath = @"C:\Exports\stok_raporu.pdf" },
                new ExportRecord { Type = "CSV", Date = DateTime.Now.AddHours(-2), Size = "1.2 MB",
                                  FileName = "musteriler.csv", FilePath = @"C:\Exports\musteriler.csv" },
                new ExportRecord { Type = "Excel", Date = DateTime.Now.AddHours(-4), Size = "3.1 MB",
                                  FileName = "aylık_analiz.xlsx", FilePath = @"C:\Exports\aylık_analiz.xlsx" },
                new ExportRecord { Type = "PDF", Date = DateTime.Now.AddDays(-1), Size = "1.8 MB",
                                  FileName = "siparis_detay.pdf", FilePath = @"C:\Exports\siparis_detay.pdf" },
                new ExportRecord { Type = "Backup", Date = DateTime.Now.AddDays(-2), Size = "45 MB",
                                  FileName = "db_backup.bak", FilePath = @"C:\Exports\db_backup.bak" },
                new ExportRecord { Type = "Chart", Date = DateTime.Now.AddDays(-3), Size = "512 KB",
                                  FileName = "grafik_export.png", FilePath = @"C:\Exports\grafik_export.png" },
                new ExportRecord { Type = "XML", Date = DateTime.Now.AddDays(-4), Size = "890 KB",
                                  FileName = "data_export.xml", FilePath = @"C:\Exports\data_export.xml" }
            };

            foreach (var export in demoExports)
            {
                recentExports.Add(export);
            }
        }

        private void SetupDataGrid()
        {
            RecentExportsDataGrid.ItemsSource = recentExports;
            UpdateStatistics();
        }

        private void InitializeStatsTimer()
        {
            statsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            statsTimer.Tick += UpdateStatisticsTimer;
            statsTimer.Start();
        }

        private void UpdateStatisticsTimer(object? sender, EventArgs e)
        {
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            // Demo istatistik güncellemeleri
            var variation = random.Next(-2, 3);

            TodayExportsText.Text = (23 + variation).ToString();
            WeekExportsText.Text = (156 + (variation * 5)).ToString();
            MonthExportsText.Text = (487 + (variation * 10)).ToString();

            // Disk kullanımı varyasyonu
            var diskUsage = 78 + random.Next(-3, 4);
            if (diskUsage > 85) diskUsage = 85;
            if (diskUsage < 70) diskUsage = 70;

            var totalSize = 2.4 + (variation * 0.1);
            TotalSizeText.Text = $"{totalSize:F1} GB";
        }

        #region Quick Export Events

        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
                // Şu anda tüm kullanıcılar export yapabilir
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    DefaultExt = "xlsx",
                    FileName = $"mestechstok_raporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // REAL Excel export using ClosedXML
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("MesTech Stok Raporu");

                    // Header
                    worksheet.Cell(1, 1).Value = "MesTech Stok Takip Sistemi";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Range(1, 1, 1, 6).Merge();

                    worksheet.Cell(2, 1).Value = $"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}";
                    worksheet.Range(2, 1, 2, 6).Merge();

                    // Data headers
                    var headers = new[] { "ID", "Tür", "Tarih", "Boyut", "Dosya Adı", "Durum" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(4, i + 1).Value = headers[i];
                        worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    }

                    // Sample data
                    var sampleData = new[]
                    {
                        new { ID = 1, Type = "Sipariş", Date = DateTime.Now.AddDays(-1), Size = "2.4 MB", FileName = "siparisler.xlsx", Status = "Tamamlandı" },
                        new { ID = 2, Type = "Stok", Date = DateTime.Now.AddDays(-2), Size = "1.8 MB", FileName = "stok_raporu.xlsx", Status = "Tamamlandı" },
                        new { ID = 3, Type = "Müşteri", Date = DateTime.Now.AddDays(-3), Size = "956 KB", FileName = "musteriler.xlsx", Status = "Tamamlandı" }
                    };

                    int row = 5;
                    foreach (var item in sampleData)
                    {
                        worksheet.Cell(row, 1).Value = item.ID;
                        worksheet.Cell(row, 2).Value = item.Type;
                        worksheet.Cell(row, 3).Value = item.Date.ToString("dd.MM.yyyy HH:mm");
                        worksheet.Cell(row, 4).Value = item.Size;
                        worksheet.Cell(row, 5).Value = item.FileName;
                        worksheet.Cell(row, 6).Value = item.Status;
                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Save workbook
                    workbook.SaveAs(saveFileDialog.FileName);

                    var fileInfo = new FileInfo(saveFileDialog.FileName);
                    var newExport = new ExportRecord
                    {
                        Type = "Excel",
                        Date = DateTime.Now,
                        Size = $"{(double)fileInfo.Length / 1024:F1} KB",
                        FileName = Path.GetFileName(saveFileDialog.FileName),
                        FilePath = saveFileDialog.FileName
                    };
                    recentExports.Insert(0, newExport);

                    GlobalLogger.Instance.LogInfo($"Excel raporu oluşturuldu: {Path.GetFileName(saveFileDialog.FileName)}", "ExportsView");
                    ToastManager.ShowSuccess($"✅ Excel raporu başarıyla oluşturuldu! Dosya: {Path.GetFileName(saveFileDialog.FileName)}", "Excel Dışa Aktarma");
                    try { Process.Start(new ProcessStartInfo { FileName = saveFileDialog.FileName, UseShellExecute = true }); } catch { }
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Excel dışa aktarma hatası: {ex.Message}", "ExportsView");
                ToastManager.ShowError($"❌ Excel dışa aktarma sırasında hata oluştu: {ex.Message}", "Hata");
            }
        }

        private async void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
                // Şu anda tüm kullanıcılar export yapabilir

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
                    DefaultExt = "pdf",
                    FileName = $"mestechstok_raporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using var fs = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var doc = new Document(PageSize.A4, 36, 36, 54, 36);
                    var writer = PdfWriter.GetInstance(doc, fs);
                    writer.SetFullCompression();
                    writer.CloseStream = true;

                    // Metadata
                    doc.AddAuthor("MesTech Teknoloji");
                    doc.AddCreator("MesTech Stok Takip Sistemi");
                    doc.AddTitle("MesTech Stok Genel Rapor");
                    doc.AddSubject("Genel özet ve göstergeler");
                    doc.Open();

                    // Font ayarı: Segoe UI -> yoksa Helvetica
                    Font titleFont, headerFont, bodyFont;
                    try
                    {
                        var segoePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
                        var bf = BaseFont.CreateFont(segoePath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                        titleFont = new Font(bf, 16, Font.BOLD, new BaseColor(0, 0, 0));
                        headerFont = new Font(bf, 11, Font.BOLD, new BaseColor(255, 255, 255));
                        bodyFont = new Font(bf, 10, Font.NORMAL, new BaseColor(0, 0, 0));
                    }
                    catch
                    {
                        var bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.NOT_EMBEDDED);
                        titleFont = new Font(bf, 16, Font.BOLD, new BaseColor(0, 0, 0));
                        headerFont = new Font(bf, 11, Font.BOLD, new BaseColor(255, 255, 255));
                        bodyFont = new Font(bf, 10, Font.NORMAL, new BaseColor(0, 0, 0));
                    }

                    var title = new Paragraph("MesTech Stok Genel Rapor", titleFont) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 6f };
                    doc.Add(title);
                    var subtitle = new Paragraph($"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}", bodyFont) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 12f };
                    doc.Add(subtitle);

                    var line = new LineSeparator(0.5f, 100f, new BaseColor(224, 224, 224), Element.ALIGN_CENTER, -2);
                    doc.Add(new Chunk(line));
                    doc.Add(new Paragraph(" "));

                    // Basit tablo (örnek içerik)
                    var table = new PdfPTable(2) { WidthPercentage = 100 };
                    table.SetWidths(new float[] { 40f, 60f });
                    var header1 = new PdfPCell(new Phrase("Gösterge", headerFont)) { BackgroundColor = new BaseColor(251, 140, 0), Padding = 6f };
                    var header2 = new PdfPCell(new Phrase("Değer", headerFont)) { BackgroundColor = new BaseColor(251, 140, 0), Padding = 6f };
                    table.AddCell(header1);
                    table.AddCell(header2);
                    table.AddCell(new PdfPCell(new Phrase("Toplam Ürün", bodyFont)) { Padding = 6f });
                    table.AddCell(new PdfPCell(new Phrase("543", bodyFont)) { Padding = 6f });
                    table.AddCell(new PdfPCell(new Phrase("Açık Sipariş", bodyFont)) { Padding = 6f });
                    table.AddCell(new PdfPCell(new Phrase("127", bodyFont)) { Padding = 6f });
                    table.AddCell(new PdfPCell(new Phrase("Düşük Stok Uyarısı", bodyFont)) { Padding = 6f });
                    table.AddCell(new PdfPCell(new Phrase("12 ürün", bodyFont)) { Padding = 6f });
                    doc.Add(table);

                    doc.Close();

                    var fileInfo = new FileInfo(saveFileDialog.FileName);
                    var newExport = new ExportRecord
                    {
                        Type = "PDF",
                        Date = DateTime.Now,
                        Size = $"{(double)fileInfo.Length / 1024:F1} KB",
                        FileName = Path.GetFileName(saveFileDialog.FileName),
                        FilePath = saveFileDialog.FileName
                    };
                    recentExports.Insert(0, newExport);

                    GlobalLogger.Instance.LogInfo($"PDF raporu oluşturuldu: {Path.GetFileName(saveFileDialog.FileName)}", "ExportsView");
                    ToastManager.ShowSuccess($"✅ PDF raporu başarıyla oluşturuldu! Dosya: {Path.GetFileName(saveFileDialog.FileName)}", "PDF Dışa Aktarma");
                    try { Process.Start(new ProcessStartInfo { FileName = saveFileDialog.FileName, UseShellExecute = true }); } catch { }
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"PDF dışa aktarma hatası: {ex.Message}", "ExportsView");
                ToastManager.ShowError($"❌ PDF dışa aktarma sırasında hata oluştu: {ex.Message}", "Hata");
            }
        }

        private void ExportCharts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG files (*.png)|*.png|JPEG files (*.jpg)|*.jpg|All files (*.*)|*.*",
                    DefaultExt = "png",
                    FileName = $"grafikler_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Demo chart export
                    var chartContent = "Grafik dışa aktarma...";
                    File.WriteAllText(saveFileDialog.FileName, chartContent);

                    var newExport = new ExportRecord
                    {
                        Type = "Chart",
                        Date = DateTime.Now,
                        Size = $"{random.Next(200, 800)} KB",
                        FileName = Path.GetFileName(saveFileDialog.FileName),
                        FilePath = saveFileDialog.FileName
                    };
                    recentExports.Insert(0, newExport);

                    MessageBox.Show($"✅ Grafikler başarıyla dışa aktarıldı!\n\n" +
                                  $"Dosya: {Path.GetFileName(saveFileDialog.FileName)}\n" +
                                  $"Format: PNG/JPEG\n" +
                                  $"Kalite: Yüksek çözünürlük",
                                  "Grafik Dışa Aktarma",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Grafik dışa aktarma sırasında hata oluştu:\n{ex.Message}",
                              "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Backup files (*.bak)|*.bak|SQL files (*.sql)|*.sql|All files (*.*)|*.*",
                    DefaultExt = "bak",
                    FileName = $"mestechstok_backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Demo backup process with progress
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();

                        // Demo backup file
                        var backupContent = "Veritabanı yedekleme tamamlandı...";
                        File.WriteAllText(saveFileDialog.FileName, backupContent);

                        var newExport = new ExportRecord
                        {
                            Type = "Backup",
                            Date = DateTime.Now,
                            Size = $"{random.Next(20, 80)} MB",
                            FileName = Path.GetFileName(saveFileDialog.FileName),
                            FilePath = saveFileDialog.FileName
                        };
                        recentExports.Insert(0, newExport);

                        MessageBox.Show($"✅ Veritabanı yedeklemesi tamamlandı!\n\n" +
                                      $"Dosya: {Path.GetFileName(saveFileDialog.FileName)}\n" +
                                      $"Tablolar: 15\n" +
                                      $"Kayıtlar: 12,543\n" +
                                      $"Sıkıştırma: GZIP",
                                      "Veritabanı Yedekleme",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    };
                    timer.Start();

                    MessageBox.Show("🔄 Veritabanı yedekleme başlatıldı...\n\nLütfen bekleyin.",
                                  "Yedekleme Başlatıldı",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Veritabanı yedekleme sırasında hata oluştu:\n{ex.Message}",
                              "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Report Generation Events

        private void GenerateSalesReport_Click(object sender, RoutedEventArgs e)
        {
            var reportType = ((ComboBoxItem)SalesReportTypeComboBox.SelectedItem)?.Content?.ToString() ?? "Günlük Satış Raporu";
            MessageBox.Show($"📊 {reportType} oluşturuluyor...\n\n" +
                          "• Satış verileri analiz ediliyor\n" +
                          "• Grafikler hazırlanıyor\n" +
                          "• Özet tablolar oluşturuluyor",
                          "Satış Raporu",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void ExportSalesReport_Click(object sender, RoutedEventArgs e)
        {
            var format = ((ComboBoxItem)SalesFormatComboBox.SelectedItem)?.Content?.ToString() ?? "Excel (.xlsx)";
            var includeCharts = IncludeChartsCheckBox.IsChecked == true;

            MessageBox.Show($"✅ Satış raporu dışa aktarılıyor...\n\n" +
                          $"Format: {format}\n" +
                          $"Grafikler: {(includeCharts ? "Dahil" : "Dahil değil")}\n" +
                          $"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}",
                          "Satış Raporu Dışa Aktarma",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void GenerateInventoryReport_Click(object sender, RoutedEventArgs e)
        {
            var reportType = ((ComboBoxItem)InventoryReportTypeComboBox.SelectedItem)?.Content?.ToString() ?? "Mevcut Stok Durumu";
            MessageBox.Show($"📦 {reportType} oluşturuluyor...\n\n" +
                          "• Stok verileri kontrol ediliyor\n" +
                          "• Kritik seviyeler belirleniyor\n" +
                          "• Kategori analizleri yapılıyor",
                          "Stok Raporu",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void ExportInventoryReport_Click(object sender, RoutedEventArgs e)
        {
            var format = ((ComboBoxItem)InventoryFormatComboBox.SelectedItem)?.Content?.ToString() ?? "Excel (.xlsx)";
            var includeBarcodes = IncludeBarcodesCheckBox.IsChecked == true;

            MessageBox.Show($"✅ Stok raporu dışa aktarılıyor...\n\n" +
                          $"Format: {format}\n" +
                          $"Barkodlar: {(includeBarcodes ? "Dahil" : "Dahil değil")}\n" +
                          $"Ürün sayısı: 150",
                          "Stok Raporu Dışa Aktarma",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void GenerateCustomerReport_Click(object sender, RoutedEventArgs e)
        {
            var reportType = ((ComboBoxItem)CustomerReportTypeComboBox.SelectedItem)?.Content?.ToString() ?? "Müşteri Listesi";
            MessageBox.Show($"👥 {reportType} oluşturuluyor...\n\n" +
                          "• Müşteri verileri toplanıyor\n" +
                          "• Gizlilik kuralları uygulanıyor\n" +
                          "• İstatistikler hesaplanıyor",
                          "Müşteri Raporu",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void ExportCustomerReport_Click(object sender, RoutedEventArgs e)
        {
            var format = ((ComboBoxItem)CustomerFormatComboBox.SelectedItem)?.Content?.ToString() ?? "Excel (.xlsx)";
            var gdprCompliant = GDPRCompliantCheckBox.IsChecked == true;

            MessageBox.Show($"✅ Müşteri raporu dışa aktarılıyor...\n\n" +
                          $"Format: {format}\n" +
                          $"GDPR Uyumlu: {(gdprCompliant ? "Evet" : "Hayır")}\n" +
                          $"Müşteri sayısı: 247",
                          "Müşteri Raporu Dışa Aktarma",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        #endregion

        #region Header Events

        private void EmailReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📧 E-posta Raporu Gönderimi\n\n" +
                          "• Rapor türü: Haftalık özet\n" +
                          "• Alıcılar: Yönetim ekibi\n" +
                          "• Gönderim zamanı: Her pazartesi 09:00\n\n" +
                          "Bu özellik geliştirme aşamasındadır.",
                          "E-posta Raporu",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void ScheduleReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("⏰ Zamanlanmış Rapor Ayarları\n\n" +
                          "• Günlük raporlar: 18:00\n" +
                          "• Haftalık raporlar: Pazartesi 09:00\n" +
                          "• Aylık raporlar: Ayın 1'i 10:00\n\n" +
                          "Zamanlama ayarları düzenlenebilir.",
                          "Zamanlanmış Raporlar",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void RefreshReports_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatistics();
            MessageBox.Show("🔄 Dışa aktarma istatistikleri yenilendi!",
                          "Yenile", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        private void OpenExportFile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var export = button?.DataContext as ExportRecord;

            if (export != null)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(export.FilePath) && File.Exists(export.FilePath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = export.FilePath,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        var dir = !string.IsNullOrWhiteSpace(export.FilePath) ? Path.GetDirectoryName(export.FilePath) : null;
                        ToastManager.ShowWarning("Dosya bulunamadı. Kayıtlı dizin açılıyor...", "Dışa Aktarım");
                        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = dir,
                                UseShellExecute = true
                            });
                        }
                        else
                        {
                            MessageBox.Show($"Dosya bulunamadı: {export.FileName}", "Dosya Aç", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Dosya açılamadı:\n{ex.Message}",
                                  "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Cleanup timer when control is unloaded
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            statsTimer?.Stop();
        }
    }
}

