using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// BulkCargoLabelDialog - Toplu kargo etiketi yazdirma penceresi
    /// </summary>
    public partial class BulkCargoLabelDialog : Window
    {
        private readonly ObservableCollection<ShipmentLabelItem> _items = new();

        // No App.ServiceProvider — D-11 pattern
        public BulkCargoLabelDialog()
        {
            InitializeComponent();
            ShipmentsGrid.ItemsSource = _items;
            LoadSampleShipments();
        }

        private void LoadSampleShipments()
        {
            _items.Add(new ShipmentLabelItem { OrderId = "TY-1234567", CustomerName = "Ahmet Kaya", Platform = "Trendyol", TrackingNumber = "" });
            _items.Add(new ShipmentLabelItem { OrderId = "TY-1234568", CustomerName = "Fatma Demir", Platform = "Trendyol", TrackingNumber = "" });
            _items.Add(new ShipmentLabelItem { OrderId = "HB-9876543", CustomerName = "Mehmet Yilmaz", Platform = "Hepsiburada", TrackingNumber = "" });
            UpdateSummary();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items) item.IsSelected = true;
            ShipmentsGrid.Items.Refresh();
            UpdateSummary();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items) item.IsSelected = false;
            ShipmentsGrid.Items.Refresh();
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var count = _items.Count(i => i.IsSelected);
            SelectionSummary.Text = $"{count} siparis secildi";
        }

        private void PrintLabels_Click(object sender, RoutedEventArgs e)
        {
            var selected = _items.Where(i => i.IsSelected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Lutfen etiket yazdirilacak siparis seciniz.", "Uyari",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var format = FormatZpl.IsChecked == true ? "ZPL"
                       : FormatPdf.IsChecked == true ? "PDF" : "PNG";
            var provider = (CargoProvider.SelectedItem as ComboBoxItem)?.Content?.ToString()
                         ?? "Yurtici Kargo";

            MessageBox.Show(
                $"{selected.Count} siparis icin {provider} {format} etiketi hazirlaniyor.\n" +
                "(H25'te IAutoShipmentService entegrasyonu tamamlanacak)",
                "Etiket Olusturuluyor", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }

    internal sealed class ShipmentLabelItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string OrderId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Platform { get; set; } = "";
        public string TrackingNumber { get; set; } = "";

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
