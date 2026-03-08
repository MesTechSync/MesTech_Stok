using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;

namespace MesTechStok.Desktop.Views
{
    public partial class InvoiceManagementView : UserControl
    {
        private readonly IInvoiceProvider? _invoiceProvider;
        private readonly TrendyolAdapter? _trendyolAdapter;
        private readonly ObservableCollection<InvoiceDisplayItem> _invoices = new();

        public InvoiceManagementView()
        {
            InitializeComponent();
            InvStartDate.SelectedDate = DateTime.Today.AddMonths(-1);
            InvEndDate.SelectedDate = DateTime.Today;

            _invoiceProvider = App.ServiceProvider?.GetService<IInvoiceProvider>();
            _trendyolAdapter = App.ServiceProvider?.GetService<TrendyolAdapter>();

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

            if (_trendyolAdapter == null)
            {
                MessageBox.Show("Trendyol adapter bulunamadi. Baglanti ekranindan yapilandirin.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sent = 0;
            foreach (var item in InvoicesGrid.SelectedItems.Cast<InvoiceDisplayItem>().ToList())
            {
                try
                {
                    // Use invoice link approach
                    var ok = await _trendyolAdapter.SendInvoiceLinkAsync(
                        item.GibInvoiceId, $"https://efatura.gov.tr/view/{item.GibInvoiceId}");
                    if (ok) { item.Status = "Gonderildi"; sent++; }
                }
                catch { /* Skip failed, continue with others */ }
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
