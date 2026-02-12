using System;
using System.Windows;

namespace MesTechStok.Desktop.Views
{
    public partial class BarcodeIntegrationDialog : Window
    {
        public string ScannedBarcode { get; set; } = "";

        public BarcodeIntegrationDialog()
        {
            InitializeComponent();
            BarcodeTextBox.Focus();
        }

        private void UseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(BarcodeTextBox.Text))
                    throw new Exception("Barkod boş olamaz!");

                ScannedBarcode = BarcodeTextBox.Text.Trim();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Giriş Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}