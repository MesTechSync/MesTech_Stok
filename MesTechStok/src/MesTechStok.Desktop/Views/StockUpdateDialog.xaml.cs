using System;
using System.Windows;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Views
{
    public partial class StockUpdateDialog : Window
    {
        public int NewStock { get; set; }

        public StockUpdateDialog(ProductItem product)
        {
            InitializeComponent();
            NewStock = product.Stock;
            LoadData(product);
        }

        private void LoadData(ProductItem product)
        {
            TitleText.Text = $"Ürün: {product.Name}";
            CurrentStockText.Text = $"Mevcut Stok: {product.Stock} adet";
            NewStockTextBox.Text = NewStock.ToString();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (int.TryParse(NewStockTextBox.Text, out var stock))
                {
                    if (stock < 0)
                        throw new Exception("Stok miktarı negatif olamaz!");

                    NewStock = stock;
                    DialogResult = true;
                }
                else
                {
                    throw new Exception("Geçersiz stok değeri!");
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