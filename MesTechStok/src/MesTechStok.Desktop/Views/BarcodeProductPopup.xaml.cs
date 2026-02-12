using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MesTechStok.Core.Data.Models;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// BarcodeProductPopup.xaml için interaction logic
    /// </summary>
    public partial class BarcodeProductPopup : Window
    {
        private Product? _product;

        public BarcodeProductPopup(Product product)
        {
            InitializeComponent();
            _product = product;
            LoadProductData();
        }

        private void LoadProductData()
        {
            if (_product == null) return;

            // Ürün adı
            ProductName.Text = _product.Name;

            // Barkod ve SKU
            BarcodeText.Text = _product.Barcode;
            SkuText.Text = _product.SKU ?? "-";

            // Fiyatlar
            PurchasePrice.Text = $"₺{_product.PurchasePrice:N2}";
            SalePrice.Text = $"₺{_product.SalePrice:N2}";

            // Stok durumu
            StockText.Text = _product.Stock.ToString();
            UpdateStockStatus();

            // Kategori ve Marka
            CategoryText.Text = _product.Category?.Name ?? "-";
            BrandText.Text = _product.Brand ?? "-";

            // Açıklama
            if (!string.IsNullOrWhiteSpace(_product.Description))
            {
                DescriptionLabel.Visibility = Visibility.Visible;
                DescriptionText.Text = _product.Description;
                DescriptionText.Visibility = Visibility.Visible;
            }

            // Ürün resmi
            LoadProductImage();
        }

        private void LoadProductImage()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_product?.ImageUrl))
                {
                    BitmapImage? image = null;

                    // URL veya dosya yolu kontrolü
                    if (_product.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        image = new BitmapImage(new Uri(_product.ImageUrl));
                    }
                    else
                    {
                        // Yerel dosya yolu
                        var imagePath = _product.ImageUrl;

                        // Göreceli yol ise tam yola çevir
                        if (!Path.IsPathRooted(imagePath))
                        {
                            // Önce ürün klasörüne bak
                            var productImagesDir = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "MesTechStok", "ProductImages", _product.Id.ToString());

                            var productImagePath = Path.Combine(productImagesDir, imagePath);
                            if (File.Exists(productImagePath))
                            {
                                imagePath = productImagePath;
                            }
                            else
                            {
                                // Genel images klasörüne bak
                                var appImagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                                var appImagePath = Path.Combine(appImagesDir, imagePath);
                                if (File.Exists(appImagePath))
                                {
                                    imagePath = appImagePath;
                                }
                            }
                        }

                        if (File.Exists(imagePath))
                        {
                            image = new BitmapImage();
                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.UriSource = new Uri(imagePath, UriKind.Absolute);
                            image.EndInit();
                            image.Freeze();
                        }
                    }

                    if (image != null)
                    {
                        ProductImage.Source = image;
                        NoImageText.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        ShowNoImage();
                    }
                }
                else
                {
                    ShowNoImage();
                }
            }
            catch
            {
                ShowNoImage();
            }
        }

        private void ShowNoImage()
        {
            ProductImage.Visibility = Visibility.Collapsed;
            NoImageText.Visibility = Visibility.Visible;
        }

        private void UpdateStockStatus()
        {
            if (_product == null) return;

            // Stok durumuna göre renklendirme
            if (_product.Stock <= 0)
            {
                StockBorder.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                StockText.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                StockText.Text += " (Stokta Yok!)";
            }
            else if (_product.Stock <= _product.MinimumStock)
            {
                StockBorder.Background = new SolidColorBrush(Color.FromRgb(255, 245, 225));
                StockText.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                StockText.Text += " (Kritik Seviye!)";
            }
            else
            {
                StockBorder.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                StockText.Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60));
                StockText.Text += " (Yeterli)";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_product != null)
            {
                // Product'ı ProductItem'a dönüştür
                var productItem = new ProductItem
                {
                    Id = _product.Id,
                    Name = _product.Name,
                    Barcode = _product.Barcode,
                    Category = _product.Category?.Name ?? "",
                    Sku = _product.SKU ?? "",
                    Price = _product.SalePrice,
                    Stock = _product.Stock,
                    ImageUrl = _product.ImageUrl
                };

                // Ürün düzenleme popup'ını aç
                var editPopup = new ProductEditDialog(productItem);
                editPopup.Owner = this;
                if (editPopup.ShowDialog() == true)
                {
                    // Güncellenen değerleri al
                    _product.Name = editPopup.ProductName;
                    _product.Barcode = editPopup.ProductBarcode;
                    _product.SKU = editPopup.ProductSku;
                    _product.SalePrice = editPopup.ProductPrice;
                    _product.Stock = editPopup.ProductStock;
                    _product.ImageUrl = editPopup.ProductImageUrl;

                    LoadProductData();
                    ToastManager.ShowSuccess($"{_product.Name} güncellendi!", "Güncelleme");
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public Product? GetProduct()
        {
            return _product;
        }
    }
}
