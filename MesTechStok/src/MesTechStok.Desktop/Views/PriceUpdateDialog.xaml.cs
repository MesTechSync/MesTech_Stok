using System;
using System.Globalization;
using System.Windows;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Views
{
    public partial class PriceUpdateDialog : Window
    {
        private bool _isSaving = false;
        public decimal NewPrice { get; set; }

        public PriceUpdateDialog(ProductItem product)
        {
            InitializeComponent();
            NewPrice = product.Price;
            LoadData(product);
        }

        private void LoadData(ProductItem product)
        {
            TitleText.Text = $"Ürün: {product.Name}";
            CurrentPriceText.Text = $"Mevcut Fiyat: ₺{product.Price:F2}";
            NewPriceTextBox.Text = NewPrice.ToString("F2");
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;
            _isSaving = true;
            if (UpdateButton != null) { UpdateButton.IsEnabled = false; UpdateButton.Content = "Kaydediliyor..."; }

            try
            {
                var tr = CultureInfo.GetCultureInfo("tr-TR");
                if (decimal.TryParse(NewPriceTextBox.Text, NumberStyles.Number, tr, out var price))
                {
                    if (price < 0)
                        throw new Exception("Fiyat negatif olamaz!");

                    NewPrice = price;
                    DialogResult = true;
                }
                else
                {
                    throw new Exception("Geçersiz fiyat değeri!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Giriş Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _isSaving = false;
                if (UpdateButton != null) { UpdateButton.IsEnabled = true; UpdateButton.Content = "✅ Güncelle"; }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PriceUpdateDialog] CancelButton error: {ex.Message}");
            }
        }
    }
}