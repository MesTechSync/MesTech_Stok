using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Desktop.Models;
// Core.Data eliminated — using MediatR CQRS (H30)
using MesTechStok.Desktop.Services;

namespace MesTechStok.Desktop.Views
{
    public partial class ProductEditDialog : Window
    {
        private bool _isSaving = false;
        public string ProductName { get; set; } = "";
        public string ProductBarcode { get; set; } = "";
        public string ProductCategory { get; set; } = "";
        public string ProductSku { get; set; } = "";
        public decimal ProductPrice { get; set; }
        public int ProductStock { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? AdditionalImageUrls { get; set; }

        public ProductEditDialog(string? initialBarcode = null)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(initialBarcode))
            {
                ProductBarcode = initialBarcode;
                BarcodeTextBox.Text = initialBarcode;
            }
            // Async loading - fire and forget with proper error handling
            LoadDataAsync();
        }

        public ProductEditDialog(ProductItem product)
        {
            InitializeComponent();
            ProductName = product.Name;
            ProductBarcode = product.Barcode;
            ProductCategory = product.Category;
            ProductSku = product.Sku;
            ProductPrice = product.Price;
            ProductStock = product.Stock;
            ProductImageUrl = product.ImageUrl;
            // Async loading - fire and forget with proper error handling
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDataAsync error: {ex.Message}");
            }
        }

        private async Task LoadData()
        {
            try
            {
                // Kategorileri MediatR CQRS ile yükle - THREADING-SAFE
                var sp = MesTechStok.Desktop.App.Services;
                if (sp != null)
                {
                    using var scope = sp.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                    var cats = await mediator.Send(new MesTech.Application.Queries.GetCategories.GetCategoriesQuery(ActiveOnly: true));
                    var categories = cats.OrderBy(c => c.Name).Select(c => c.Name).ToList();

                    // UI thread üzerinde güncelle
                    Dispatcher.Invoke(() =>
                    {
                        CategoryComboBox.ItemsSource = categories;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadData hata: {ex.Message}");
            }

            // UI alanlarını doldur
            NameTextBox.Text = ProductName;
            BarcodeTextBox.Text = ProductBarcode;
            SkuTextBox.Text = ProductSku;
            if (!string.IsNullOrWhiteSpace(ProductCategory))
            {
                CategoryComboBox.Text = ProductCategory;
                CategoryComboBox.SelectedItem = ProductCategory;
            }
            PriceTextBox.Text = ProductPrice.ToString("F2");
            StockTextBox.Text = ProductStock.ToString();
            ImageTextBox.Text = ProductImageUrl ?? string.Empty;
            AdditionalImagesTextBox.Text = AdditionalImageUrls ?? string.Empty;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;
            _isSaving = true;
            if (SaveButton != null) { SaveButton.IsEnabled = false; SaveButton.Content = "Kaydediliyor..."; }

            try
            {
                ProductName = (NameTextBox.Text ?? string.Empty).Trim();
                ProductBarcode = (BarcodeTextBox.Text ?? string.Empty).Trim();
                ProductSku = (SkuTextBox.Text ?? string.Empty).Trim();
                ProductCategory = (CategoryComboBox.Text ?? string.Empty).Trim();
                ProductImageUrl = (ImageTextBox.Text ?? string.Empty).Trim();

                // TR kültüründe fiyat parse
                var tr = CultureInfo.GetCultureInfo("tr-TR");
                if (decimal.TryParse(PriceTextBox.Text, NumberStyles.Number, tr, out var price))
                    ProductPrice = price;
                else
                    throw new Exception("Geçersiz fiyat değeri!");

                if (int.TryParse(StockTextBox.Text, out var stock))
                    ProductStock = Math.Max(0, stock);
                else
                    throw new Exception("Geçersiz stok değeri!");

                if (string.IsNullOrWhiteSpace(ProductName))
                    throw new Exception("Ürün adı boş olamaz!");

                if (string.IsNullOrWhiteSpace(ProductBarcode))
                    throw new Exception("Barkod boş olamaz!");

                // EAN-13 temel doğrulama (yalnızca rakam ve 13 hane)
                if (!(ProductBarcode.All(char.IsDigit) && ProductBarcode.Length == 13))
                    throw new Exception("Barkod EAN-13 formatında olmalı (13 hane, sadece rakam)!");

                AdditionalImageUrls = (AdditionalImagesTextBox.Text ?? string.Empty).Trim();
                // Görsel dosyası seçildiyse depoya kopyala ve thumbnail üret
                try
                {
                    if (!string.IsNullOrWhiteSpace(ProductImageUrl))
                    {
                        var storage = App.Services?.GetService<ImageStorageService>() ?? new ImageStorageService();
                        // Geçici olarak 0 id; gerçek kayıttan sonra güncellenebilir. Düzenle modunda Id var.
                        Guid id = Guid.Empty;
                        if (Owner is MainWindow mw && mw.DataContext is object) { }
                        // Not: Kaydetten sonra servis id'yi verdiğinde tekrar üretim yapılabilir.
                        await storage.SaveAsync(id, ProductImageUrl);
                    }
                }
                catch
                {
                    // Intentional: image storage save is non-critical — dialog result is not blocked by image failure.
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Giriş Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _isSaving = false;
                if (SaveButton != null) { SaveButton.IsEnabled = true; SaveButton.Content = "✅ Kaydet"; }
            }
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Ürün görseli seçin",
                    Filter = "Görseller|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Tüm Dosyalar|*.*"
                };
                if (ofd.ShowDialog() == true)
                {
                    ImageTextBox.Text = ofd.FileName;
                }
            }
            catch
            {
                // Intentional: UI event handler (file dialog) — dialog may throw on unusual system states.
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
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); _ = LoadDataAsync(); }
    }
}