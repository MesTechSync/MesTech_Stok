using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views
{
    public partial class PlatformOrdersView : UserControl
    {
        public PlatformOrdersView()
        {
            InitializeComponent();
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-7);
            EndDatePicker.SelectedDate = DateTime.Today;
        }

        private void RefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            // TODO: TrendyolAdapter.PullOrdersAsync() + OpenCartAdapter.PullOrdersAsync()
            MessageBox.Show("Siparis yenileme icin TrendyolAdapter/OpenCartAdapter implementasyonu bekleniyor (DEV3).",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InvoiceSelected_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItems.Count == 0)
            {
                MessageBox.Show("Lutfen faturalanacak siparisleri seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var count = OrdersGrid.SelectedItems.Count;
            var result = MessageBox.Show(
                $"{count} siparis icin fatura olusturulacak. Devam edilsin mi?",
                "Fatura Olustur", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // TODO: IInvoiceProvider uzerinden fatura olusturma akisi
                MessageBox.Show($"{count} siparis icin fatura olusturma islemi baslatildi.",
                    "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: DataGrid filtreleme
        }

        private void SearchChanged(object sender, TextChangedEventArgs e)
        {
            // TODO: Arama filtreleme
        }

        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Secili siparis detayi gosterme
        }
    }
}
