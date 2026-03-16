using System;
using System.Windows;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Views
{
    public partial class StockUpdateDialog : Window
    {
        private bool _isSaving = false;
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
            if (_isSaving) return;
            _isSaving = true;
            if (UpdateButton != null) { UpdateButton.IsEnabled = false; UpdateButton.Content = "Kaydediliyor..."; }

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
            finally
            {
                _isSaving = false;
                if (UpdateButton != null) { UpdateButton.IsEnabled = true; UpdateButton.Content = "✅ Güncelle"; }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); }
    }
}