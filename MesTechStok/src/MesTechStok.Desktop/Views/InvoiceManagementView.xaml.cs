using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MesTech.Application.Interfaces;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Views
{
    public partial class InvoiceManagementView : UserControl
    {
        private readonly IInvoiceProvider? _invoiceProvider;
        private readonly IInvoiceCapableAdapter? _invoiceCapableAdapter;
        private readonly ObservableCollection<InvoiceDisplayItem> _invoices = new();

        // Null defaults allow the WPF designer to call the constructor without arguments.
        public InvoiceManagementView(IInvoiceProvider? invoiceProvider = null, IInvoiceCapableAdapter? invoiceCapableAdapter = null)
        {
            InitializeComponent();
            InvStartDate.SelectedDate = DateTime.Today.AddMonths(-1);
            InvEndDate.SelectedDate = DateTime.Today;

            _invoiceProvider = invoiceProvider;
            _invoiceCapableAdapter = invoiceCapableAdapter;

            InvoicesGrid.ItemsSource = _invoices;
            LoadSampleData();
        }

        private async void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceProvider == null)
            {
                MessageBox.Show("Fatura servisi bulunamadi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var invoice = new InvoiceDto(
                InvoiceNumber: $"FTR{DateTime.Now:yyyyMMdd}{(_invoices.Count + 1):D3}",
                CustomerName: "Test Musteri",
                CustomerTaxNumber: "1234567890",
                CustomerTaxOffice: "Istanbul",
                CustomerAddress: "Test Adres, Istanbul",
                SubTotal: 1000m,
                TaxTotal: 200m,
                GrandTotal: 1200m,
                Lines: new[]
                {
                    new InvoiceLineDto("Ornek Urun", "SKU001", 2, 500m, 20m, 200m, 1200m)
                }
            );

            try
            {
                var result = await _invoiceProvider.CreateEFaturaAsync(invoice);
                if (result.Success)
                {
                    _invoices.Insert(0, new InvoiceDisplayItem
                    {
                        InvoiceNumber = invoice.InvoiceNumber,
                        InvoiceType = "e-Fatura",
                        CustomerName = invoice.CustomerName,
                        TaxNumber = invoice.CustomerTaxNumber ?? "",
                        InvoiceDate = DateTime.Now,
                        SubTotal = invoice.SubTotal,
                        TaxTotal = invoice.TaxTotal,
                        GrandTotal = invoice.GrandTotal,
                        Status = "Taslak",
                        PlatformCode = "-",
                        GibInvoiceId = result.GibInvoiceId ?? ""
                    });
                    UpdateStats();
                    MessageBox.Show($"Fatura olusturuldu. GIB ID: {result.GibInvoiceId}",
                        "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Fatura olusturulamadi: {result.ErrorMessage}",
                        "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatura olusturma hatasi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is not InvoiceDisplayItem selected)
            {
                MessageBox.Show("Lutfen PDF indirilecek faturayi seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_invoiceProvider == null) { MessageBox.Show("Fatura servisi bulunamadi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            try
            {
                var pdfBytes = await _invoiceProvider.GetPdfAsync(selected.GibInvoiceId);
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{selected.InvoiceNumber}.pdf");
                await File.WriteAllBytesAsync(path, pdfBytes);
                MessageBox.Show($"PDF kaydedildi: {path}", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF indirme hatasi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SendToPlatform_Click(object sender, RoutedEventArgs e)
        {
            if (InvoicesGrid.SelectedItems.Count == 0)
            {
                MessageBox.Show("Lutfen platforma gonderilecek faturalari seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var count = InvoicesGrid.SelectedItems.Count;
            var result = MessageBox.Show(
                $"{count} fatura ilgili platforma gonderilecek.\nDevam edilsin mi?",
                "Platforma Gonder", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            if (_invoiceCapableAdapter == null)
            {
                MessageBox.Show("Platform adapter bulunamadi. Baglanti ekranindan yapilandirin.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sent = 0;
            foreach (var item in InvoicesGrid.SelectedItems.Cast<InvoiceDisplayItem>().ToList())
            {
                try
                {
                    // Use invoice link approach
                    var ok = await _invoiceCapableAdapter.SendInvoiceLinkAsync(
                        item.GibInvoiceId, $"https://efatura.gov.tr/view/{item.GibInvoiceId}");
                    if (ok) { item.Status = "Gonderildi"; sent++; }
                }
                catch (Exception ex)
                {
                    GlobalLogger.Instance.LogError($"Platform gonderi hatasi ({item.GibInvoiceId}): {ex.Message}", "InvoiceManagement");
                }
            }

            InvoicesGrid.Items.Refresh();
            UpdateStats();
            MessageBox.Show($"{sent}/{count} fatura platforma gonderildi.", "Sonuc", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (InvoicesGrid?.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(InvoicesGrid.ItemsSource);
            if (view == null) return;

            var typeFilter = (InvoiceTypeFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tumu";
            var statusFilter = (InvoiceStatusFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tumu";
            var searchText = InvoiceSearchBox?.Text?.Trim()?.ToLowerInvariant() ?? "";

            view.Filter = obj =>
            {
                if (obj is not InvoiceDisplayItem item) return false;

                if (typeFilter != "Tumu" && item.InvoiceType != typeFilter)
                    return false;

                if (statusFilter != "Tumu" && item.Status != statusFilter)
                    return false;

                if (!string.IsNullOrEmpty(searchText))
                {
                    return (item.InvoiceNumber?.ToLowerInvariant().Contains(searchText) == true) ||
                           (item.CustomerName?.ToLowerInvariant().Contains(searchText) == true);
                }

                return true;
            };

            UpdateStats();
        }

        private void LoadSampleData()
        {
            // Pre-populate with sample data so the screen isn't empty on first load
            var samples = new[]
            {
                new InvoiceDisplayItem { InvoiceNumber = "FTR20260301001", InvoiceType = "e-Fatura", CustomerName = "ABC Ticaret A.S.", TaxNumber = "3456789012", InvoiceDate = DateTime.Today.AddDays(-5), SubTotal = 5000, TaxTotal = 1000, GrandTotal = 6000, Status = "Onaylandi", PlatformCode = "Trendyol", GibInvoiceId = "GIB20260301001" },
                new InvoiceDisplayItem { InvoiceNumber = "FTR20260302002", InvoiceType = "e-Arsiv", CustomerName = "Mehmet Yilmaz", TaxNumber = "12345678901", InvoiceDate = DateTime.Today.AddDays(-3), SubTotal = 850, TaxTotal = 170, GrandTotal = 1020, Status = "Gonderildi", PlatformCode = "Trendyol", GibInvoiceId = "ARS20260302001" },
                new InvoiceDisplayItem { InvoiceNumber = "FTR20260305003", InvoiceType = "e-Fatura", CustomerName = "XYZ Bilisim Ltd.", TaxNumber = "3987654321", InvoiceDate = DateTime.Today.AddDays(-1), SubTotal = 12500, TaxTotal = 2500, GrandTotal = 15000, Status = "Taslak", PlatformCode = "OpenCart", GibInvoiceId = "GIB20260305001" },
            };

            foreach (var s in samples) _invoices.Add(s);
            UpdateStats();
        }

        private void UpdateStats()
        {
            TotalInvoicesText.Text = _invoices.Count.ToString();
            DraftInvoicesText.Text = _invoices.Count(i => i.Status == "Taslak").ToString();
            SentInvoicesText.Text = _invoices.Count(i => i.Status == "Gonderildi").ToString();
            AcceptedInvoicesText.Text = _invoices.Count(i => i.Status == "Onaylandi").ToString();
            RejectedInvoicesText.Text = _invoices.Count(i => i.Status == "Reddedildi").ToString();
            TotalAmountText.Text = $"{_invoices.Sum(i => i.GrandTotal):N2} TL";
        }

        private async void BulkCreate_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Faturasi kesilmemis 3 siparis bulundu.\nTumunu e-Arsiv olarak kesilsin mi?",
                "Toplu Fatura Kes", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            if (_invoiceProvider == null)
            {
                MessageBox.Show("Fatura servisi yapilandirilmamis.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int created = 0, failed = 0;
            var pendingOrders = new[] { "ORD001", "ORD002", "ORD003" };
            foreach (var orderId in pendingOrders)
            {
                try
                {
                    var dto = new InvoiceDto(
                        InvoiceNumber: $"FTR{DateTime.Now:yyyyMMdd}{(_invoices.Count + created + 1):D3}",
                        CustomerName: $"Siparis Sahibi ({orderId})",
                        CustomerTaxNumber: null,
                        CustomerTaxOffice: null,
                        CustomerAddress: "Adres",
                        SubTotal: 500m, TaxTotal: 100m, GrandTotal: 600m,
                        Lines: new[] { new InvoiceLineDto("Urun", null, 1, 500m, 20m, 100m, 600m) });

                    var invResult = await _invoiceProvider.CreateEArsivAsync(dto);
                    if (invResult.Success)
                    {
                        _invoices.Insert(0, new InvoiceDisplayItem
                        {
                            InvoiceNumber = dto.InvoiceNumber, InvoiceType = "e-Arsiv",
                            CustomerName = dto.CustomerName, TaxNumber = "",
                            InvoiceDate = DateTime.Now, SubTotal = dto.SubTotal,
                            TaxTotal = dto.TaxTotal, GrandTotal = dto.GrandTotal,
                            Status = "Gonderildi", PlatformCode = "-", GibInvoiceId = invResult.GibInvoiceId ?? ""
                        });
                        created++;
                    }
                    else { failed++; }
                }
                catch (Exception ex)
                {
                    GlobalLogger.Instance.LogError($"Toplu fatura hatasi ({orderId}): {ex.Message}", "InvoiceManagement");
                    failed++;
                }
            }
            UpdateStats();
            MessageBox.Show($"Toplu fatura tamamlandi: {created} basarili, {failed} hatali.", "Sonuc", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void RefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            var selected = InvoicesGrid.SelectedItems.Cast<InvoiceDisplayItem>().ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Durumu guncellenecek faturayi seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_invoiceProvider == null)
            {
                MessageBox.Show("Fatura servisi yapilandirilmamis. Lutfen baglanti ayarlarini kontrol ediniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int updated = 0;
            foreach (var item in selected)
            {
                if (string.IsNullOrEmpty(item.GibInvoiceId)) continue;
                try
                {
                    var status = await _invoiceProvider.CheckStatusAsync(item.GibInvoiceId);
                    item.Status = status.Status;
                    updated++;
                }
                catch (Exception ex)
                {
                    GlobalLogger.Instance.LogError($"Durum guncelleme hatasi ({item.GibInvoiceId}): {ex.Message}", "InvoiceManagement");
                }
            }
            InvoicesGrid.Items.Refresh();
            UpdateStats();
            MessageBox.Show($"{updated} fatura durumu guncellendi.", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void CancelInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is not InvoiceDisplayItem selected)
            {
                MessageBox.Show("Iptal edilecek faturayi seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_invoiceProvider == null)
            {
                MessageBox.Show("Fatura servisi yapilandirilmamis. Lutfen baglanti ayarlarini kontrol ediniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Fatura {selected.InvoiceNumber} iptal edilecek. Emin misiniz?",
                "Fatura Iptal", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var cancelResult = await _invoiceProvider.CancelInvoiceAsync(selected.GibInvoiceId);
                if (cancelResult.Success)
                {
                    selected.Status = "Iptal";
                    InvoicesGrid.Items.Refresh();
                    UpdateStats();
                    MessageBox.Show("Fatura iptal edildi.", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                    MessageBox.Show($"Iptal hatasi: {cancelResult.ErrorMessage}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Iptal hatasi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem is not InvoiceDisplayItem selected ||
                string.IsNullOrEmpty(selected.GibInvoiceId) ||
                _invoiceProvider == null)
                return;

            try
            {
                var pdfBytes = await _invoiceProvider.GetPdfAsync(selected.GibInvoiceId);
                var tempPath = Path.Combine(Path.GetTempPath(), $"{selected.InvoiceNumber}.pdf");
                await File.WriteAllBytesAsync(tempPath, pdfBytes);
                PdfWebBrowser.Navigate(tempPath);

                PdfPanelColumn.Width = new System.Windows.GridLength(350);
                PdfPreviewBorder.Visibility = System.Windows.Visibility.Visible;
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"PDF onizleme hatasi: {ex.Message}", "InvoiceManagement");
            }
        }

        private void ClosePdfPanel_Click(object sender, RoutedEventArgs e)
        {
            PdfWebBrowser.Navigate("about:blank");  // Release file lock first
            PdfPanelColumn.Width = new System.Windows.GridLength(0);
            PdfPreviewBorder.Visibility = System.Windows.Visibility.Collapsed;
        }
    }

    internal class InvoiceDisplayItem
    {
        public string InvoiceNumber { get; set; } = "";
        public string InvoiceType { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string TaxNumber { get; set; } = "";
        public DateTime InvoiceDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public string Status { get; set; } = "";
        public string PlatformCode { get; set; } = "";
        public string GibInvoiceId { get; set; } = "";
    }
}
