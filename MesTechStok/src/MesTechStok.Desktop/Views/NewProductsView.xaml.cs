using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Models;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Views
{
    public partial class NewProductsView : UserControl
    {
        private readonly ObservableCollection<ProductItem> _allProducts;
        private readonly CollectionViewSource _productsViewSource;
        private ProductItem? _selectedProduct;

        public NewProductsView()
        {
            InitializeComponent();

            _allProducts = new ObservableCollection<ProductItem>();
            _productsViewSource = new CollectionViewSource { Source = _allProducts };

            ProductsDataGrid.ItemsSource = _productsViewSource.View;

            LoadDemoProducts();
            UpdateStatistics();
            SetupSearchBox();
        }

        private void SetupSearchBox()
        {
            try
            {
                if (SearchTextBox != null)
                {
                    // Placeholder ayarla
                    SearchTextBox.Text = "Ürün adı, barkod veya kategori ara...";
                    SearchTextBox.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Gray);
                    SearchTextBox.FontStyle = FontStyles.Italic;

                    // Event handlers
                    SearchTextBox.GotFocus += SearchTextBox_GotFocus;
                    SearchTextBox.LostFocus += SearchTextBox_LostFocus;
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"SearchBox setup error: {ex.Message}", "NewProductsView");
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SearchTextBox.Text == "Ürün adı, barkod veya kategori ara...")
                {
                    SearchTextBox.Text = "";
                    SearchTextBox.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
                    SearchTextBox.FontStyle = FontStyles.Normal;
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"SearchBox focus error: {ex.Message}", "NewProductsView");
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    SearchTextBox.Text = "Ürün adı, barkod veya kategori ara...";
                    SearchTextBox.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Gray);
                    SearchTextBox.FontStyle = FontStyles.Italic;
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"SearchBox lost focus error: {ex.Message}", "NewProductsView");
            }
        }

        private void LoadDemoProducts()
        {
            var demoProducts = new List<ProductItem>
            {
                new() { Id = 1, Barcode = "1234567890123", Name = "Coca Cola 330ml", Category = "İçecek", Price = 5.50m, Stock = 25 },
                new() { Id = 2, Barcode = "9876543210987", Name = "Doritos Nacho 150g", Category = "Atıştırmalık", Price = 12.75m, Stock = 18 },
                new() { Id = 3, Barcode = "5555555555555", Name = "Samsung Galaxy S24", Category = "Elektronik", Price = 35000.00m, Stock = 3 },
                new() { Id = 4, Barcode = "1111111111111", Name = "Nivea Krem 100ml", Category = "Kozmetik", Price = 25.90m, Stock = 42 },
                new() { Id = 5, Barcode = "2222222222222", Name = "Adidas Spor Ayakkabı", Category = "Spor", Price = 850.00m, Stock = 7 },
                new() { Id = 6, Barcode = "3333333333333", Name = "MacBook Pro 14\"", Category = "Elektronik", Price = 75000.00m, Stock = 1 },
                new() { Id = 7, Barcode = "4444444444444", Name = "Nutella 750g", Category = "Gıda", Price = 89.90m, Stock = 33 },
                new() { Id = 8, Barcode = "6666666666666", Name = "iPhone 15 Pro", Category = "Elektronik", Price = 55000.00m, Stock = 2 },
                new() { Id = 9, Barcode = "7777777777777", Name = "Lego City Set", Category = "Oyuncak", Price = 299.99m, Stock = 12 },
                new() { Id = 10, Barcode = "8888888888888", Name = "Sony WH-1000XM5", Category = "Elektronik", Price = 1200.00m, Stock = 8 }
            };

            foreach (var product in demoProducts)
            {
                _allProducts.Add(product);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CategoryFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            try
            {
                // Null check for UI controls during initialization
                if (CategoryFilterComboBox == null || SearchTextBox == null || _productsViewSource?.View == null)
                    return;

                var view = _productsViewSource.View;

                if (view.CanFilter)
                {
                    view.Filter = FilterProducts;
                    UpdateStatistics();
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"ApplyFilters error: {ex.Message}", "NewProductsView");
            }
        }

        private bool FilterProducts(object item)
        {
            if (item is not ProductItem product) return false;

            var selectedCategory = (CategoryFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "Tüm Kategoriler")
            {
                if (!product.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            var searchText = SearchTextBox.Text;
            if (!string.IsNullOrEmpty(searchText) && searchText != "Ürün adı, barkod veya kategori ara...")
            {
                return product.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                       product.Barcode.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                       product.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        private void UpdateStatistics()
        {
            try
            {
                // Safely cast only ProductItem objects, filtering out any other types
                var visibleProducts = _productsViewSource.View
                    .OfType<ProductItem>()  // Use OfType instead of Cast to avoid casting errors
                    .ToList();

                // Null check for UI elements
                if (TotalProductsText == null || LowStockText == null || TotalValueText == null ||
                    AveragePriceText == null || CriticalStockText == null)
                    return;

                TotalProductsText.Text = visibleProducts.Count.ToString();
                LowStockText.Text = visibleProducts.Count(p => p.Stock <= 10 && p.Stock > 0).ToString();

                if (visibleProducts.Any())
                {
                    var totalValue = visibleProducts.Sum(p => p.Price * p.Stock);
                    var averagePrice = visibleProducts.Average(p => p.Price);
                    var criticalStock = visibleProducts.Count(p => p.Stock <= 5);

                    TotalValueText.Text = $"₺{totalValue:N0}";
                    AveragePriceText.Text = $"₺{averagePrice:F2}";
                    CriticalStockText.Text = $"{criticalStock} ürün";
                }
                else
                {
                    TotalValueText.Text = "₺0";
                    AveragePriceText.Text = "₺0.00";
                    CriticalStockText.Text = "0 ürün";
                }
            }
            catch (Exception ex)
            {
                // Graceful error handling
                GlobalLogger.Instance.LogError($"UpdateStatistics error: {ex.Message}", "NewProductsView");
            }
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedProduct = ProductsDataGrid.SelectedItem as ProductItem;

            if (_selectedProduct != null)
            {
                ShowSelectedProductDetails(_selectedProduct);
                SelectedProductPanel.Visibility = Visibility.Visible;
                ProductDetailsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                SelectedProductPanel.Visibility = Visibility.Collapsed;
                ProductDetailsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowSelectedProductDetails(ProductItem product)
        {
            SelectedProductName.Text = product.Name;
            SelectedProductBarcode.Text = $"Barkod: {product.Barcode}";
            SelectedProductCategory.Text = $"Kategori: {product.Category}";
            SelectedProductPrice.Text = $"Fiyat: ₺{product.Price:F2}";
            SelectedProductStock.Text = $"Stok: {product.Stock} adet";
        }

        // Event Handlers - sadece MessageBox göster
        private void RefreshProducts_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("✅ Ürün listesi yenilendi!", "Yenileme", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ProductEditDialog();
                if (dialog.ShowDialog() == true)
                {
                    var newProduct = new ProductItem
                    {
                        Id = _allProducts.Any() ? _allProducts.Max(p => p.Id) + 1 : 1,
                        Name = dialog.ProductName,
                        Barcode = dialog.ProductBarcode,
                        Category = dialog.ProductCategory,
                        Price = dialog.ProductPrice,
                        Stock = dialog.ProductStock
                    };

                    _allProducts.Add(newProduct);
                    UpdateStatistics();
                    ToastManager.ShowSuccess($"✅ Yeni ürün eklendi!\n{newProduct.Name}", "Ürün");
                    GlobalLogger.Instance.LogInfo($"Yeni ürün eklendi: {newProduct.Name}", "NewProductsView");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Ürün ekleme hatası: {ex.Message}", "Ürün");
                GlobalLogger.Instance.LogError($"Product add error: {ex.Message}", "NewProductsView");
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var categoryDialog = CreateCategoryDialog();
                if (categoryDialog.ShowDialog() == true)
                {
                    // Kategori eklendi, ComboBox'ı güncelle
                    UpdateCategoryComboBox();
                    ToastManager.ShowSuccess("Yeni kategori eklendi!", "Kategori");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Kategori ekleme hatası: {ex.Message}", "Kategori");
                GlobalLogger.Instance.LogError($"Category add error: {ex.Message}", "NewProductsView");
            }
        }

        private Window CreateCategoryDialog()
        {
            var dialog = new Window
            {
                Title = "Yeni Kategori Ekle",
                Width = 500,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250)),
                ResizeMode = ResizeMode.NoResize
            };

            var mainGrid = new Grid { Margin = new Thickness(20) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
            headerStack.Children.Add(new TextBlock { Text = "📂", FontSize = 24, Margin = new Thickness(0, 0, 10, 0) });
            headerStack.Children.Add(new TextBlock
            {
                Text = "Yeni Kategori Ekle",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            headerBorder.Child = headerStack;
            Grid.SetRow(headerBorder, 0);
            mainGrid.Children.Add(headerBorder);

            // Form content
            var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var formStack = new StackPanel();

            // Category Name
            formStack.Children.Add(new Label { Content = "Kategori Adı*", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var categoryNameTB = new TextBox
            {
                Name = "CategoryNameTextBox",
                Height = 35,
                FontSize = 14,
                Padding = new Thickness(10, 8, 10, 8),
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15)
            };
            formStack.Children.Add(categoryNameTB);

            // Category Description
            formStack.Children.Add(new Label { Content = "Kategori Açıklaması", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var categoryDescTB = new TextBox
            {
                Name = "CategoryDescriptionTextBox",
                Height = 80,
                FontSize = 12,
                Padding = new Thickness(10, 8, 10, 8),
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 15)
            };
            formStack.Children.Add(categoryDescTB);

            // Meta Title
            formStack.Children.Add(new Label { Content = "Meta Başlığı", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var metaTitleTB = new TextBox
            {
                Name = "MetaTitleTextBox",
                Height = 35,
                FontSize = 14,
                Padding = new Thickness(10, 8, 10, 8),
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15)
            };
            formStack.Children.Add(metaTitleTB);

            // Meta Description
            formStack.Children.Add(new Label { Content = "Meta Açıklaması", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var metaDescTB = new TextBox
            {
                Name = "MetaDescriptionTextBox",
                Height = 60,
                FontSize = 12,
                Padding = new Thickness(10, 8, 10, 8),
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Margin = new Thickness(0, 0, 0, 15)
            };
            formStack.Children.Add(metaDescTB);

            // Keywords
            formStack.Children.Add(new Label { Content = "Anahtar Kelimeler", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var keywordsTB = new TextBox
            {
                Name = "KeywordsTextBox",
                Height = 35,
                FontSize = 12,
                Padding = new Thickness(10, 8, 10, 8),
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 5)
            };
            formStack.Children.Add(keywordsTB);
            formStack.Children.Add(new TextBlock
            {
                Text = "Virgül ile ayırarak yazın (örn: elektronik, telefon, samsung)",
                FontSize = 10,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 15)
            });

            scrollViewer.Content = formStack;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // Buttons
            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var cancelBtn = new Button
            {
                Content = "❌ İptal",
                Width = 80,
                Height = 35,
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 10, 0)
            };
            cancelBtn.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };

            var saveBtn = new Button
            {
                Content = "✅ Kaydet",
                Width = 80,
                Height = 35,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold
            };

            saveBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(categoryNameTB.Text))
                {
                    ToastManager.ShowWarning("Kategori adı gereklidir!", "Kategori");
                    categoryNameTB.Focus();
                    return;
                }

                // Save category data (simulated)
                var categoryData = new
                {
                    Name = categoryNameTB.Text.Trim(),
                    Description = categoryDescTB.Text.Trim(),
                    MetaTitle = metaTitleTB.Text.Trim(),
                    MetaDescription = metaDescTB.Text.Trim(),
                    Keywords = keywordsTB.Text.Trim()
                };

                GlobalLogger.Instance.LogInfo($"Yeni kategori eklendi: {categoryData.Name}", "CategoryManager");
                dialog.DialogResult = true;
                dialog.Close();
            };

            buttonStack.Children.Add(cancelBtn);
            buttonStack.Children.Add(saveBtn);

            Grid.SetRow(buttonStack, 2);
            mainGrid.Children.Add(buttonStack);

            dialog.Content = mainGrid;
            return dialog;
        }

        private void UpdateCategoryComboBox()
        {
            try
            {
                if (CategoryFilterComboBox != null)
                {
                    // Mevcut seçimi koru
                    var selectedItem = CategoryFilterComboBox.SelectedItem;

                    // ComboBox'ı yenile (simulated)
                    // Gerçek uygulamada veritabanından kategoriler yüklenecek

                    GlobalLogger.Instance.LogInfo("Kategori ComboBox güncellendi", "NewProductsView");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"ComboBox update error: {ex.Message}", "NewProductsView");
            }
        }

        private void AddWithBarcode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var barcodeDialog = new BarcodeIntegrationDialog();
                if (barcodeDialog.ShowDialog() == true && !string.IsNullOrEmpty(barcodeDialog.ScannedBarcode))
                {
                    var dialog = new ProductEditDialog(barcodeDialog.ScannedBarcode);
                    if (dialog.ShowDialog() == true)
                    {
                        var newProduct = new ProductItem
                        {
                            Id = _allProducts.Any() ? _allProducts.Max(p => p.Id) + 1 : 1,
                            Name = dialog.ProductName,
                            Barcode = dialog.ProductBarcode,
                            Category = dialog.ProductCategory,
                            Price = dialog.ProductPrice,
                            Stock = dialog.ProductStock
                        };

                        _allProducts.Add(newProduct);
                        UpdateStatistics();
                        ToastManager.ShowSuccess($"✅ Barkodlu ürün eklendi!\nBarkod: {newProduct.Barcode}\nÜrün: {newProduct.Name}", "Ürün");
                        GlobalLogger.Instance.LogInfo($"Barkodlu ürün eklendi: {newProduct.Name} - {newProduct.Barcode}", "NewProductsView");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Barkodlu ürün ekleme hatası: {ex.Message}", "Ürün");
                GlobalLogger.Instance.LogError($"Barcode product add error: {ex.Message}", "NewProductsView");
            }
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
            {
                ToastManager.ShowWarning("Lütfen düzenlemek için bir ürün seçin!", "Ürün");
                return;
            }

            try
            {
                var dialog = new ProductEditDialog(_selectedProduct);
                if (dialog.ShowDialog() == true)
                {
                    _selectedProduct.Name = dialog.ProductName;
                    _selectedProduct.Barcode = dialog.ProductBarcode;
                    _selectedProduct.Category = dialog.ProductCategory;
                    _selectedProduct.Price = dialog.ProductPrice;
                    _selectedProduct.Stock = dialog.ProductStock;

                    _productsViewSource.View.Refresh();
                    UpdateStatistics();
                    ShowSelectedProductDetails(_selectedProduct);
                    ToastManager.ShowSuccess($"✅ Ürün güncellendi!\n{_selectedProduct.Name}", "Ürün");
                    GlobalLogger.Instance.LogInfo($"Ürün güncellendi: {_selectedProduct.Name}", "NewProductsView");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Ürün düzenleme hatası: {ex.Message}", "Ürün");
                GlobalLogger.Instance.LogError($"Product edit error: {ex.Message}", "NewProductsView");
            }
        }

        private void UpdateStock_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
            {
                ToastManager.ShowWarning("Lütfen stok güncellemek için bir ürün seçin!", "Stok");
                return;
            }

            try
            {
                var dialog = new StockUpdateDialog(_selectedProduct);
                if (dialog.ShowDialog() == true)
                {
                    _selectedProduct.Stock = dialog.NewStock;
                    _productsViewSource.View.Refresh();
                    UpdateStatistics();
                    ShowSelectedProductDetails(_selectedProduct);
                    ToastManager.ShowSuccess($"✅ Stok güncellendi!\n{_selectedProduct.Name}\nYeni Stok: {_selectedProduct.Stock} adet", "Stok");
                    GlobalLogger.Instance.LogInfo($"Stok güncellendi: {_selectedProduct.Name} - {_selectedProduct.Stock}", "NewProductsView");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Stok güncelleme hatası: {ex.Message}", "Stok");
                GlobalLogger.Instance.LogError($"Stock update error: {ex.Message}", "NewProductsView");
            }
        }

        private void UpdatePrice_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🏷️ Fiyat güncelleme!", "Fiyat", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🗑️ Ürün silme!", "Sil", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImportFromExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📤 Excel'den içe aktarma!", "İçe Aktar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📊 Excel'e dışa aktarma!", "Dışa Aktar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("⏮️ İlk sayfa!", "Sayfa", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("⏪ Önceki sayfa!", "Sayfa", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("⏩ Sonraki sayfa!", "Sayfa", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LastPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("⏭️ Son sayfa!", "Sayfa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // ProductItem moved to ProductsView.xaml.cs to avoid duplication
}
