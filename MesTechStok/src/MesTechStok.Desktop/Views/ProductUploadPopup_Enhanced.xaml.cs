using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MesTechStok.Desktop.Models;
using MesTechStok.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// ProductUploadPopup_Enhanced.xaml için etkileşim mantığı
    /// A++++ Premium Glassmorphism tasarımı ile geliştirilmiş ürün yükleme popup'ı
    /// </summary>
    public partial class ProductUploadPopup_Enhanced : Window
    {
        // Temporarily commented out - barcode service will be restored later
        private readonly IBarcodeService? _barcodeService;
        private ProductItem? _productItem;

        public ProductUploadPopup_Enhanced()
        {
            InitializeComponent();

            // Dependency Injection ile servis al
            try
            {
                // Temporarily commented out - barcode service will be restored later
                _barcodeService = App.ServiceProvider?.GetService<IBarcodeService>();
            }
            catch (Exception ex)
            {
                // Log error if needed
                System.Diagnostics.Debug.WriteLine($"BarcodeService dependency injection hatası: {ex.Message}");
            }

            InitializeDefaults();
        }

        public ProductUploadPopup_Enhanced(ProductItem? existingProduct = null) : this()
        {
            _productItem = existingProduct;
            if (_productItem != null)
            {
                LoadProductData(_productItem);
            }
        }

        private void InitializeDefaults()
        {
            // Varsayılan değerleri ayarla
            BarcodeTextBox.Focus();
        }

        private void LoadProductData(ProductItem product)
        {
            try
            {
                BarcodeTextBox.Text = product.Barcode ?? "";
                ProductNameTextBox.Text = product.Name ?? "";
                PriceTextBox.Text = product.Price.ToString("F2");
                StockQuantityTextBox.Text = product.Stock.ToString();
                DescriptionTextBox.Text = product.Description ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ürün verileri yüklenirken hata: {ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #region Event Handlers

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(BarcodeTextBox.Text))
                {
                    MessageBox.Show("Barkod alanı boş olamaz.", "Validation Hatası",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    BarcodeTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(ProductNameTextBox.Text))
                {
                    MessageBox.Show("Ürün adı alanı boş olamaz.", "Validation Hatası",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ProductNameTextBox.Focus();
                    return;
                }

                // Price validation
                if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
                {
                    MessageBox.Show("Geçerli bir fiyat giriniz.", "Validation Hatası",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PriceTextBox.Focus();
                    return;
                }

                // Stock validation
                if (!int.TryParse(StockQuantityTextBox.Text, out int stock) || stock < 0)
                {
                    MessageBox.Show("Geçerli bir stok miktarı giriniz.", "Validation Hatası",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    StockQuantityTextBox.Focus();
                    return;
                }

                // Create or update product
                if (_productItem == null)
                {
                    _productItem = new ProductItem();
                }

                _productItem.Barcode = BarcodeTextBox.Text.Trim();
                _productItem.Name = ProductNameTextBox.Text.Trim();
                _productItem.Price = price;
                _productItem.Stock = stock;
                _productItem.Category = ""; // Category removed for now
                _productItem.Description = DescriptionTextBox.Text?.Trim() ?? "";
                _productItem.LastUpdated = DateTime.Now;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydetme sırasında hata: {ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BarcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Enter basıldığında otomatik ürün bilgilerini getir
                var barcode = BarcodeTextBox.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(barcode))
                {
                    // Barkoddan ürün bilgilerini getirmeyi dene
                    ProductNameTextBox.Focus();
                }
            }
        }

        private async void ScanBarcodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_barcodeService != null)
                {
                    // Barkod tarama işlemini başlat
                    var scannedBarcode = await _barcodeService.ScanBarcodeAsync();
                    if (!string.IsNullOrWhiteSpace(scannedBarcode))
                    {
                        BarcodeTextBox.Text = scannedBarcode;
                        ProductNameTextBox.Focus();
                    }
                }
                else
                {
                    MessageBox.Show("Barkod servisi kullanılamıyor.", "Hata",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Barkod tarama hatası: {ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Public Properties

        public ProductItem? ProductResult => _productItem;

        #endregion

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // ESC ile kapatma
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            // Ctrl+S ile kaydetme
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveButton_Click(sender, new RoutedEventArgs());
            }
        }
    }
}
