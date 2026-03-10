using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MesTech.Application.Interfaces;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Views
{
    public partial class IncomingInvoicesView : UserControl
    {
        private readonly IInvoiceProvider? _invoiceProvider;
        private readonly ObservableCollection<IncomingInvoiceItem> _invoices = new();

        // Optional params — WPF designer compatibility
        public IncomingInvoicesView(IInvoiceProvider? invoiceProvider = null)
        {
            InitializeComponent();
            _invoiceProvider = invoiceProvider;
            InvStartDate.SelectedDate = DateTime.Today.AddMonths(-1);
            InvEndDate.SelectedDate = DateTime.Today;
            IncomingGrid.ItemsSource = _invoices;
            LoadMockData();  // Always load mock data — real API pending DEV 1
        }

        private void LoadMockData()
        {
            _invoices.Clear();
            _invoices.Add(new IncomingInvoiceItem { InvoiceNumber = "GF-2026-001", SenderName = "ABC Tedarikci Ltd.", VKN = "3456789012", InvoiceDate = DateTime.Today.AddDays(-2), GrandTotal = 12500m, Status = "Bekliyor", Platform = "Trendyol", GibInvoiceId = "GIB-GF-001" });
            _invoices.Add(new IncomingInvoiceItem { InvoiceNumber = "GF-2026-002", SenderName = "XYZ Lojistik A.S.", VKN = "9876543210", InvoiceDate = DateTime.Today.AddDays(-1), GrandTotal = 3200m, Status = "Kabul Edildi", Platform = "Hepsiburada", GibInvoiceId = "GIB-GF-002" });
            _invoices.Add(new IncomingInvoiceItem { InvoiceNumber = "GF-2026-003", SenderName = "DEF Tedarik San.", VKN = "1122334455", InvoiceDate = DateTime.Today, GrandTotal = 7800m, Status = "Bekliyor", Platform = "OpenCart", GibInvoiceId = "GIB-GF-003" });
            UpdateStats();
        }

        private void UpdateStats()
        {
            TotalText.Text = _invoices.Count.ToString();
            PendingText.Text = _invoices.Count(i => i.Status == "Bekliyor").ToString();
            AcceptedText.Text = _invoices.Count(i => i.Status == "Kabul Edildi").ToString();
            RejectedText.Text = _invoices.Count(i => i.Status == "Reddedildi").ToString();
        }

        private void RefreshInvoices_Click(object sender, RoutedEventArgs e)
        {
            // Provider integration pending (IInvoiceProvider.GetIncomingInvoicesAsync not yet in interface)
            // For now, reload mock data
            LoadMockData();
        }

        private void AcceptInvoice_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not IncomingInvoiceItem item) return;
            if (item.Status != "Bekliyor")
            {
                MessageBox.Show("Bu fatura zaten islenmis.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            item.Status = "Kabul Edildi";
            IncomingGrid.Items.Refresh();
            UpdateStats();
            MessageBox.Show($"Fatura {item.InvoiceNumber} kabul edildi.", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RejectInvoice_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not IncomingInvoiceItem item) return;
            if (item.Status != "Bekliyor")
            {
                MessageBox.Show("Bu fatura zaten islenmis.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var confirm = MessageBox.Show($"Fatura {item.InvoiceNumber} reddedilecek. Emin misiniz?",
                "Fatura Reddet", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            item.Status = "Reddedildi";
            IncomingGrid.Items.Refresh();
            UpdateStats();
            MessageBox.Show($"Fatura {item.InvoiceNumber} reddedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not IncomingInvoiceItem item) return;
            if (_invoiceProvider == null)
            {
                MessageBox.Show("Fatura servisi yapilandirilmamis.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var pdfBytes = await _invoiceProvider.GetPdfAsync(item.GibInvoiceId);
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{item.InvoiceNumber}.pdf");
                await File.WriteAllBytesAsync(path, pdfBytes);
                MessageBox.Show($"PDF kaydedildi: {path}", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"PDF indirme hatasi ({item.GibInvoiceId}): {ex.Message}", "IncomingInvoices");
            }
        }

        private void IncomingGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { /* future use */ }
    }

    internal class IncomingInvoiceItem
    {
        public string InvoiceNumber { get; set; } = "";
        public string SenderName { get; set; } = "";
        public string VKN { get; set; } = "";
        public DateTime InvoiceDate { get; set; }
        public decimal GrandTotal { get; set; }
        public string Status { get; set; } = "";
        public string Platform { get; set; } = "";
        public string GibInvoiceId { get; set; } = "";
    }
}
