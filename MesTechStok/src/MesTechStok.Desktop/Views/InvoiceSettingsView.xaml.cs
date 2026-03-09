using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace MesTechStok.Desktop.Views
{
    public partial class InvoiceSettingsView : UserControl
    {
        private byte[]? _logoBytes;
        private byte[]? _signatureBytes;

        // No App.ServiceProvider — D-11 pattern
        public InvoiceSettingsView()
        {
            InitializeComponent();
        }

        private void AutoEmail_Checked(object sender, RoutedEventArgs e)
            => AccountingEmailPanel.Visibility = Visibility.Visible;

        private void AutoEmail_Unchecked(object sender, RoutedEventArgs e)
            => AccountingEmailPanel.Visibility = Visibility.Collapsed;

        private void UploadLogo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Logo Secin",
                Filter = "Gorsel Dosyalari (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dialog.ShowDialog() != true) return;

            _logoBytes = File.ReadAllBytes(dialog.FileName);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = new MemoryStream(_logoBytes);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            LogoPreview.Source = bmp;
            LogoFileName.Text = Path.GetFileName(dialog.FileName);
        }

        private void UploadSignature_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Imza Secin",
                Filter = "Gorsel Dosyalari (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };
            if (dialog.ShowDialog() != true) return;

            _signatureBytes = File.ReadAllBytes(dialog.FileName);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = new MemoryStream(_signatureBytes);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            SignaturePreview.Source = bmp;
            SignatureFileName.Text = Path.GetFileName(dialog.FileName);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var kdvRates = new[] { 0, 1, 10, 20 };
            var idx = DefaultKdvRate.SelectedIndex;
            if (idx < 0 || idx >= kdvRates.Length) return;
            var selectedKdv = kdvRates[idx];

            MessageBox.Show(
                $"Ayarlar kaydedildi.\n" +
                $"KDV: %{selectedKdv}\n" +
                $"Istisna Kodu: {IstisnaSebebi.Text}\n" +
                $"Otomatik Kesim: {(AutoCutOnDelivery.IsChecked == true ? "Aktif" : "Pasif")}\n" +
                $"Logo: {(_logoBytes != null ? $"{_logoBytes.Length / 1024} KB" : "Yok")}\n" +
                $"Imza: {(_signatureBytes != null ? $"{_signatureBytes.Length / 1024} KB" : "Yok")}",
                "Kaydedildi", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
