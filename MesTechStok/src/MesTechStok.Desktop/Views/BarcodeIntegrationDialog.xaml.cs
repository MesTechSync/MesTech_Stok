using System;
using System.Windows;

namespace MesTechStok.Desktop.Views
{
    public partial class BarcodeIntegrationDialog : Window
    {
        private bool _isSaving = false;
        public string ScannedBarcode { get; set; } = "";

        public BarcodeIntegrationDialog()
        {
            InitializeComponent();
            BarcodeTextBox.Focus();
        }

        private void UseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;
            _isSaving = true;
            if (UseButton != null) { UseButton.IsEnabled = false; UseButton.Content = "Kaydediliyor..."; }

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
            finally
            {
                _isSaving = false;
                if (UseButton != null) { UseButton.IsEnabled = true; UseButton.Content = "✅ Bu Barkodu Kullan"; }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}