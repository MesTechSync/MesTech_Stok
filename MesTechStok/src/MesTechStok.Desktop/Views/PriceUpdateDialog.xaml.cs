using System;
using System.Globalization;
using System.Windows;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Views
{
    public partial class PriceUpdateDialog : Window
    {
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
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}