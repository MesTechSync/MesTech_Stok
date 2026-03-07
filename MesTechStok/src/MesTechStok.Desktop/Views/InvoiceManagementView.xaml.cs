using System;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views
{
    public partial class InvoiceManagementView : UserControl
    {
        public InvoiceManagementView()
        {
            InitializeComponent();
            InvStartDate.SelectedDate = DateTime.Today.AddMonths(-1);
            InvEndDate.SelectedDate = DateTime.Today;
        }

        private void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Yeni fatura olusturma dialog'u
            MessageBox.Show(
                "Yeni fatura olusturma:\n\n" +
                "1. Siparis secin veya manuel giris yapin\n" +
                "2. Alici VKN/TCKN girin\n" +
                "3. e-Fatura veya e-Arsiv secin\n" +
                "4. Kalem bilgilerini doldurun\n\n" +
                "IInvoiceProvider implementasyonu bekleniyor (DEV1 + DEV3).",
                "Yeni Fatura", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            if (InvoicesGrid.SelectedItem == null)
            {
                MessageBox.Show("Lutfen PDF indirilecek faturayi seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // TODO: IInvoiceProvider.GetPdfAsync(gibInvoiceId)
            MessageBox.Show("PDF indirme islemi baslatildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SendToPlatform_Click(object sender, RoutedEventArgs e)
        {
            if (InvoicesGrid.SelectedItems.Count == 0)
            {
                MessageBox.Show("Lutfen platforma gonderilecek faturalari seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var count = InvoicesGrid.SelectedItems.Count;
            var result = MessageBox.Show(
                $"{count} fatura ilgili platforma (Trendyol/OpenCart) gonderilecek.\nDevam edilsin mi?",
                "Platforma Gonder", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // TODO: TrendyolAdapter.SendInvoiceLinkAsync() veya SendInvoiceFileAsync()
                MessageBox.Show("Fatura gonderme islemi baslatildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: DataGrid filtreleme
        }
    }
}
