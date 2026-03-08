using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Queries.GetSidebarCategories;
using MesTech.Application.Queries.GetProductDbStatus;
using MesTech.Application.Commands.SeedDemoData;
using MesTech.Application.Commands.CreateBulkProducts;
using MesTechStok.Desktop.Models;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Utils;
using MesTechStok.Core.Services.Abstract;
using System.Diagnostics;
using WinForms = System.Windows.Forms;
using System.Windows.Input;
using MesTechStok.Desktop.Services;

namespace MesTechStok.Desktop.Views
{
    public partial class ProductsView : UserControl
    {
        private IProductDataService _productService;
        private bool _usingDemoService;
        private readonly ObservableCollection<ProductItem> _displayedProducts;
        private List<ProductItem> _allProducts = new List<ProductItem>();
        private ProductItem? _selectedProduct;
        private string _currentSearchTerm = "";
        private string _currentCategoryFilter = "";
        private ProductSortOrder _currentSortOrder = ProductSortOrder.Name;
        private bool _filterOutOfStock;
        private bool _filterLowStock;
        private bool _filterDiscounted;
        private decimal? _minPrice;
        private decimal? _maxPrice;
        private string _originFilter = "";
        private string _materialFilter = "";
        private string _sizeFilter = "";

        // Barkod dinleme
        private bool _barcodeListening; // TODO: Kullanım dışı ise kaldırılabilir

        // Authorization-bound properties
        public bool CanCreateProducts { get; private set; } = true;
        public bool CanEditProducts { get; private set; } = true;
        public bool CanDeleteProducts { get; private set; } = true;
        public bool CanUpdateStock { get; private set; } = true;
        public bool CanUpdatePrice { get; private set; } = true;

        public ProductsView()
        {
            InitializeComponent();

            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider!;
                _productService = sp.GetRequiredService<IProductDataService>();
                _usingDemoService = false;
            }
            catch
            {
                _productService = new EnhancedProductService();
                _usingDemoService = true;
            }
            _displayedProducts = new ObservableCollection<ProductItem>();

            ProductsDataGrid.ItemsSource = _displayedProducts;

            _ = InitializeAsync();
            _ = LoadCategoriesAsync(); // Kategorileri başlangıçta yükle

            // ReportsView profil köprüsü için son instance'ı kaydet
            try { ProductsViewProfilesBridge.Register(this); } catch { }

            // Popup kaydı sonrası otomatik yenile
            MesTechStok.Desktop.Utils.EventBus.ProductsChanged += async (barcode) =>
            {
                try
                {
                    await LoadProductsAsync();
                    await UpdateStatisticsAsync();
                    if (!string.IsNullOrWhiteSpace(barcode))
                    {
                        var row = _displayedProducts.FirstOrDefault(p => string.Equals(p.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
                        if (row != null)
                        {
                            ProductsDataGrid.SelectedItem = row;
                            ProductsDataGrid.ScrollIntoView(row);
                            // Geçici highlight (1.5 sn)
                            try
                            {
                                var dgRow = (DataGridRow)ProductsDataGrid.ItemContainerGenerator.ContainerFromItem(row);
                                if (dgRow != null)
                                {
                                    var original = dgRow.Background;
                                    dgRow.Background = System.Windows.Media.Brushes.LightYellow;
                                    await Task.Delay(1500);
                                    dgRow.Background = original;
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            };
        }

        private async Task InitializeAsync()
        {
            try
            {
                await SetupAuthorizationsAsync();
                SetSearchPlaceholder();
                await LoadCategoriesAsync();
                await LoadProductsAsync();
                await UpdateStatisticsAsync();
                await UpdateDbStatusAsync();
                TryLoadAutosavedColumns();
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Başlangıç hatası: {ex.Message}", "error");
            }
        }

        private async Task SetupAuthorizationsAsync()
        {
            // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
            // Şu anda tüm kullanıcılar tüm işlemleri yapabilir
            CanCreateProducts = CanEditProducts = CanDeleteProducts = CanUpdateStock = CanUpdatePrice = true;
            ApplyRbacToContextMenus();
        }

        private void ApplyRbacToContextMenus()
        {
            try
            {
                var ctxMenu = ProductsDataGrid.ContextMenu;
                if (ctxMenu != null)
                {
                    var miEdit = ctxMenu.Items.OfType<System.Windows.Controls.MenuItem>().FirstOrDefault(x => (x.Name ?? "").Equals("MenuEdit"));
                    var miStock = ctxMenu.Items.OfType<System.Windows.Controls.MenuItem>().FirstOrDefault(x => (x.Name ?? "").Equals("MenuStock"));
                    var miPrice = ctxMenu.Items.OfType<System.Windows.Controls.MenuItem>().FirstOrDefault(x => (x.Name ?? "").Equals("MenuPrice"));
                    var miDelete = ctxMenu.Items.OfType<System.Windows.Controls.MenuItem>().FirstOrDefault(x => (x.Name ?? "").Equals("MenuDelete"));
                    if (miEdit != null) miEdit.Visibility = CanEditProducts ? Visibility.Visible : Visibility.Collapsed;
                    if (miStock != null) miStock.Visibility = CanUpdateStock ? Visibility.Visible : Visibility.Collapsed;
                    if (miPrice != null) miPrice.Visibility = CanUpdatePrice ? Visibility.Visible : Visibility.Collapsed;
                    if (miDelete != null) miDelete.Visibility = CanDeleteProducts ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch { }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                // Kategori display'ini güncelle
                UpdateSelectedCategoryDisplay();

                // Sidebar kategorilerini yükle
                await LoadSidebarCategoriesAsync();
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Kategori yükleme hatası: {ex.Message}", "error");
            }
        }

        private void UpdateSelectedCategoryDisplay()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentCategoryFilter))
                {
                    SelectedCategoryDisplay.Text = "🗂️ Tüm Kategoriler";
                }
                else
                {
                    SelectedCategoryDisplay.Text = $"📂 {_currentCategoryFilter}";
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"Kategori display güncelleme hatası: {ex.Message}", "ProductsView");
            }
        }

        private async Task LoadSidebarCategoriesAsync()
        {
            try
            {
                SidebarCategoriesList.Children.Clear();

                if (!_usingDemoService)
                {
                    try
                    {
                        var sp = MesTechStok.Desktop.App.ServiceProvider;
                        if (sp != null)
                        {
                            var mediator = sp.GetRequiredService<IMediator>();
                            var categories = await mediator.Send(new GetSidebarCategoriesQuery());

                            foreach (var category in categories)
                            {
                                var categoryButton = new Button
                                {
                                    Content = $"📂 {category.Name}",
                                    Width = 280,
                                    Height = 36,
                                    Margin = new Thickness(0, 2, 0, 2),
                                    Style = FindResource("ModernButtonStyle") as Style,
                                    Tag = category.Id
                                };

                                categoryButton.Click += (s, e) => SidebarCategory_Click(category.Id, category.Name);
                                SidebarCategoriesList.Children.Add(categoryButton);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GlobalLogger.Instance.LogWarning($"Sidebar kategori yükleme hatası: {ex.Message}", "ProductsView");
                    }
                }

                // Demo kategoriler ekle
                if (_usingDemoService || SidebarCategoriesList.Children.Count == 0)
                {
                    var demoCategories = new[] { "Elektronik", "Giyim", "Ev & Yaşam", "Spor", "Kitap", "Oyuncak" };
                    foreach (var category in demoCategories)
                    {
                        var categoryButton = new Button
                        {
                            Content = $"📂 {category}",
                            Width = 280,
                            Height = 36,
                            Margin = new Thickness(0, 2, 0, 2),
                            Style = FindResource("ModernButtonStyle") as Style
                        };

                        categoryButton.Click += (s, e) => SidebarCategory_Click(Guid.Empty, category);
                        SidebarCategoriesList.Children.Add(categoryButton);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Sidebar kategori yükleme hatası: {ex.Message}", "error");
            }
        }

        private System.Threading.CancellationTokenSource? _catSearchCts;

        // Yeni Sidebar Event Handlers
        private void SidebarCategory_Click(Guid categoryId, string categoryName)
        {
            try
            {
                _currentCategoryFilter = categoryName;
                UpdateSelectedCategoryDisplay();
                SidebarActiveCategory.Text = categoryName;
                _ = LoadProductsAsync();

                // Sidebar'ı kapat
                CategorySidebarPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Kategori seçim hatası: {ex.Message}", "ProductsView");
            }
        }

        private void SelectAllCategories_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                _currentCategoryFilter = "";
                UpdateSelectedCategoryDisplay();
                SidebarActiveCategory.Text = "Tüm Kategoriler";
                _ = LoadProductsAsync();

                // Sidebar'ı kapat
                CategorySidebarPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Tüm kategoriler seçim hatası: {ex.Message}", "ProductsView");
            }
        }

        private async void SidebarCategorySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = SidebarCategorySearch.Text?.Trim() ?? "";
                await FilterSidebarCategories(searchText);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Kategori arama hatası: {ex.Message}", "ProductsView");
            }
        }

        private async Task FilterSidebarCategories(string searchText)
        {
            try
            {
                SidebarCategoriesList.Children.Clear();

                // "Tüm Kategoriler" öğesini her zaman göster
                var allCategoriesBorder = new Border
                {
                    Background = System.Windows.Media.Brushes.LightGray,
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 4),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                allCategoriesBorder.MouseLeftButtonUp += SelectAllCategories_Click;

                var allCategoriesPanel = new StackPanel { Orientation = Orientation.Horizontal };
                allCategoriesPanel.Children.Add(new TextBlock { Text = "🏠", FontSize = 14, Margin = new Thickness(0, 0, 8, 0) });
                allCategoriesPanel.Children.Add(new TextBlock { Text = "Tüm Kategoriler", FontWeight = FontWeights.Medium });
                allCategoriesBorder.Child = allCategoriesPanel;
                SidebarCategoriesList.Children.Add(allCategoriesBorder);

                // Kategorileri filtrele ve göster
                if (!_usingDemoService)
                {
                    var sp = MesTechStok.Desktop.App.ServiceProvider;
                    if (sp != null)
                    {
                        var mediator = sp.GetRequiredService<IMediator>();
                        var categories = await mediator.Send(new GetSidebarCategoriesQuery(
                            string.IsNullOrEmpty(searchText) ? null : searchText));

                        foreach (var category in categories)
                        {
                            AddSidebarCategoryButton(category.Id, category.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Kategori filtreleme hatası: {ex.Message}", "ProductsView");
            }
        }

        private void AddSidebarCategoryButton(Guid categoryId, string categoryName)
        {
            var categoryButton = new Button
            {
                Content = $"📂 {categoryName}",
                Height = 36,
                Margin = new Thickness(0, 2, 0, 2),
                Style = FindResource("ModernButtonStyle") as Style,
                Tag = categoryId,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            categoryButton.Click += (s, e) => SidebarCategory_Click(categoryId, categoryName);
            SidebarCategoriesList.Children.Add(categoryButton);
        }

        private void SidebarAddCategory_Click(object sender, RoutedEventArgs e)
        {
            // Kategori ekleme popup'ı açılacak
            ShowToastNotification("Kategori ekleme özelliği yakında eklenecek", "info");
        }

        private void SidebarEditCategory_Click(object sender, RoutedEventArgs e)
        {
            // Kategori düzenleme popup'ı açılacak
            ShowToastNotification("Kategori düzenleme özelliği yakında eklenecek", "info");
        }

        private void SidebarDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            // Kategori silme onayı ve işlemi
            ShowToastNotification("Kategori silme özelliği yakında eklenecek", "info");
        }

        private void CloseSidebar_Click(object sender, RoutedEventArgs e)
        {
            CategorySidebarPanel.Visibility = Visibility.Collapsed;
        }



        private async Task LoadProductsAsync()
        {
            try
            {
                var (currentPage, pageSize) = ProductPagination.GetPaginationParameters();
                currentPage = ProductPagination.CurrentPage;

                var result = await _productService.GetProductsPagedAsync(
                    currentPage,
                    pageSize,
                    _currentSearchTerm,
                    _currentCategoryFilter,
                    _currentSortOrder);

                // Fallback: DB boş ise demo veri ile doldur
                if (result.TotalItems == 0 && !_usingDemoService)
                {
                    _productService = new EnhancedProductService();
                    _usingDemoService = true;
                    result = await _productService.GetProductsPagedAsync(currentPage, pageSize, _currentSearchTerm, _currentCategoryFilter, _currentSortOrder);
                }

                // Tüm ürünleri _allProducts'a kaydet
                _allProducts.Clear();
                foreach (var product in result.Items)
                {
                    _allProducts.Add(product);
                }

                _displayedProducts.Clear();
                foreach (var product in result.Items)
                {
                    if (_filterOutOfStock && product.Stock != 0) continue;
                    if (_filterLowStock && !(product.Stock > 0 && product.Stock <= product.MinimumStock)) continue;
                    if (_filterDiscounted && product.DiscountRate <= 0) continue;
                    if (_minPrice.HasValue && product.SalePrice < _minPrice.Value) continue;
                    if (_maxPrice.HasValue && product.SalePrice > _maxPrice.Value) continue;
                    if (!string.IsNullOrWhiteSpace(_originFilter) && !string.Equals(product.Origin, _originFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrWhiteSpace(_materialFilter) && !string.Equals(product.Material, _materialFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrWhiteSpace(_sizeFilter))
                    {
                        var sizes = (product.Sizes ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (!sizes.Contains(_sizeFilter, StringComparer.OrdinalIgnoreCase)) continue;
                    }
                    _displayedProducts.Add(product);
                }

                ProductPagination.SetData(result.TotalItems, result.CurrentPage, result.PageSize);
                await UpdateDbStatusAsync();
                // UI güvenliği: boşsa belirgin uyarı
                if (_displayedProducts.Count == 0)
                {
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogWarning("[UI] Ürün listesi boş görünüyor (DB durumu sağlıklı). Filtreler sıfırlanmalı.", nameof(ProductsView));
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Ürün yükleme hatası: {ex.Message}", "error");
                try
                {
                    // DB erişimi yoksa otomatik demo fallback
                    if (!_usingDemoService)
                    {
                        _productService = new EnhancedProductService();
                        _usingDemoService = true;
                        var (currentPage, pageSize) = ProductPagination.GetPaginationParameters();
                        var result = await _productService.GetProductsPagedAsync(currentPage, pageSize, _currentSearchTerm, _currentCategoryFilter, _currentSortOrder);

                        // Tüm ürünleri _allProducts'a kaydet
                        _allProducts.Clear();
                        foreach (var product in result.Items)
                        {
                            _allProducts.Add(product);
                        }

                        _displayedProducts.Clear();
                        foreach (var product in result.Items)
                        {
                            if (_filterOutOfStock && product.Stock != 0) continue;
                            if (_filterLowStock && !(product.Stock > 0 && product.Stock <= product.MinimumStock)) continue;
                            if (_filterDiscounted && product.DiscountRate <= 0) continue;
                            if (_minPrice.HasValue && product.SalePrice < _minPrice.Value) continue;
                            if (_maxPrice.HasValue && product.SalePrice > _maxPrice.Value) continue;
                            _displayedProducts.Add(product);
                        }
                        ProductPagination.SetData(result.TotalItems, result.CurrentPage, result.PageSize);
                        MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo("[UI] DB erişilemedi, demo veri ile dolduruldu", nameof(ProductsView));
                    }
                }
                catch
                {
                    // Both primary and fallback failed — show error state
                    ProductsErrorText.Text = $"Ürünler yüklenemedi: {ex.Message}";
                    ProductsErrorState.Visibility = Visibility.Visible;
                }
            }
            finally
            {
                // Empty state kontrol
                try
                {
                    EmptyStatePanel.Visibility = _displayedProducts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                catch { }
            }
        }

        private async void QuickFilter_Changed(object sender, RoutedEventArgs e)
        {
            _filterOutOfStock = ChipOutOfStock.IsChecked == true;
            _filterLowStock = ChipLowStock.IsChecked == true;
            _filterDiscounted = ChipDiscounted.IsChecked == true;
            await LoadProductsAsync();
            await UpdateStatisticsAsync();
        }

        private async void AdvancedQuickFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _originFilter = GetComboValueOrEmpty(OriginFilter);
                if (_originFilter == "Menşei") _originFilter = string.Empty;
                _materialFilter = GetComboValueOrEmpty(MaterialFilter);
                if (_materialFilter == "Materyal") _materialFilter = string.Empty;
                _sizeFilter = GetComboValueOrEmpty(SizeFilter);
                if (_sizeFilter == "Beden") _sizeFilter = string.Empty;
            }
            catch { }
            await LoadProductsAsync();
            await UpdateStatisticsAsync();
        }

        private static string GetComboValueOrEmpty(ComboBox combo)
        {
            try { return (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty; } catch { return string.Empty; }
        }

        // Gelişmiş Filtreler UI
        private void BtnAdvancedFilters_Click(object sender, RoutedEventArgs e)
        {
            try { AdvancedFiltersPopup.IsOpen = true; } catch { }
        }
        private async void AdvancedFilters_Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var origins = new List<string>();
                if (AdvOriginTR.IsChecked == true) origins.Add("TR");
                if (AdvOriginCN.IsChecked == true) origins.Add("CN");
                if (AdvOriginEU.IsChecked == true) origins.Add("EU");
                if (AdvOriginUS.IsChecked == true) origins.Add("US");
                var materials = new List<string>();
                if (AdvMatPorselen.IsChecked == true) materials.Add("Porselen");
                if (AdvMatCam.IsChecked == true) materials.Add("Cam");
                if (AdvMatPlastik.IsChecked == true) materials.Add("Plastik");
                if (AdvMatMetal.IsChecked == true) materials.Add("Metal");
                var sizes = new List<string>();
                if (AdvSz2S.IsChecked == true) sizes.Add("2S");
                if (AdvSzS.IsChecked == true) sizes.Add("S");
                if (AdvSzM.IsChecked == true) sizes.Add("M");
                if (AdvSzL.IsChecked == true) sizes.Add("L");
                if (AdvSzXL.IsChecked == true) sizes.Add("XL");
                if (AdvSz2XL.IsChecked == true) sizes.Add("2XL");
                if (AdvSz3XL.IsChecked == true) sizes.Add("3XL");

                // UI üzerindeki tekli comboları temizleyelim ki çakışmasın
                OriginFilter.SelectedIndex = 0; _originFilter = string.Empty;
                MaterialFilter.SelectedIndex = 0; _materialFilter = string.Empty;
                SizeFilter.SelectedIndex = 0; _sizeFilter = string.Empty;

                // Filtre uygulama: Çoklu
                await ApplyAdvancedFiltersAsync(origins, materials, sizes);
                // Son kullanılan set olarak ekle
                var title = $"Set: {(origins.Count > 0 ? string.Join('/', origins) : "-")}|{(materials.Count > 0 ? string.Join('/', materials) : "-")}|{(sizes.Count > 0 ? string.Join('/', sizes) : "-")}";
                AddRecentFilterSet(title, origins.ToList(), materials.ToList(), sizes.ToList());
                AdvancedFiltersPopup.IsOpen = false;
            }
            catch { }
        }

        private async Task ApplyAdvancedFiltersAsync(IReadOnlyList<string> origins, IReadOnlyList<string> materials, IReadOnlyList<string> sizes)
        {
            await LoadProductsAsync();
            try
            {
                var filtered = _displayedProducts.Where(p =>
                {
                    bool ok = true;
                    if (origins.Count > 0) ok &= !string.IsNullOrWhiteSpace(p.Origin) && origins.Contains(p.Origin, StringComparer.OrdinalIgnoreCase);
                    if (materials.Count > 0) ok &= !string.IsNullOrWhiteSpace(p.Material) && materials.Contains(p.Material, StringComparer.OrdinalIgnoreCase);
                    if (sizes.Count > 0)
                    {
                        var ps = (p.Sizes ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        ok &= ps.Intersect(sizes, StringComparer.OrdinalIgnoreCase).Any();
                    }
                    return ok;
                }).ToList();

                _displayedProducts.Clear();
                foreach (var p in filtered) _displayedProducts.Add(p);
                await UpdateStatisticsAsync();

                // Chip özetlerini üret
                RenderActiveChips(origins, materials, sizes);
            }
            catch { }
        }

        private void AdvancedFilters_Reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdvOriginTR.IsChecked = AdvOriginCN.IsChecked = AdvOriginEU.IsChecked = AdvOriginUS.IsChecked = false;
                AdvMatPorselen.IsChecked = AdvMatCam.IsChecked = AdvMatPlastik.IsChecked = AdvMatMetal.IsChecked = false;
                AdvSz2S.IsChecked = AdvSzS.IsChecked = AdvSzM.IsChecked = AdvSzL.IsChecked = AdvSzXL.IsChecked = AdvSz2XL.IsChecked = AdvSz3XL.IsChecked = false;
                // Chip'leri gizle
                ActiveChipsPanel.Children.Clear();
                ActiveChipsPanel.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        private void RenderActiveChips(IReadOnlyList<string> origins, IReadOnlyList<string> materials, IReadOnlyList<string> sizes)
        {
            try
            {
                ActiveChipsPanel.Children.Clear();
                void AddChip(string label)
                {
                    var b = new Button { Content = label + "  ✕", Style = (Style)FindResource("ChipButtonStyle") };
                    b.Click += (s, e) => _ = RemoveChipAsync(label);
                    ActiveChipsPanel.Children.Add(b);
                }
                if (origins.Count > 0) AddChip("Menşei: " + string.Join(",", origins));
                if (materials.Count > 0) AddChip("Materyal: " + string.Join(",", materials));
                if (sizes.Count > 0) AddChip("Beden: " + string.Join(",", sizes));
                ActiveChipsPanel.Visibility = ActiveChipsPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        private async Task RemoveChipAsync(string label)
        {
            try
            {
                if (label.StartsWith("Menşei:")) AdvancedFilters_ResetOrigins();
                if (label.StartsWith("Materyal:")) AdvancedFilters_ResetMaterials();
                if (label.StartsWith("Beden:")) AdvancedFilters_ResetSizes();
                await ApplyAdvancedFiltersAsync(new List<string>(), new List<string>(), new List<string>());
            }
            catch { }
        }

        private void AdvancedFilters_ResetOrigins()
        {
            AdvOriginTR.IsChecked = AdvOriginCN.IsChecked = AdvOriginEU.IsChecked = AdvOriginUS.IsChecked = false;
        }
        private void AdvancedFilters_ResetMaterials()
        {
            AdvMatPorselen.IsChecked = AdvMatCam.IsChecked = AdvMatPlastik.IsChecked = AdvMatMetal.IsChecked = false;
        }
        private void AdvancedFilters_ResetSizes()
        {
            AdvSz2S.IsChecked = AdvSzS.IsChecked = AdvSzM.IsChecked = AdvSzL.IsChecked = AdvSzXL.IsChecked = AdvSz2XL.IsChecked = AdvSz3XL.IsChecked = false;
        }

        // Son kullanılan filtre setleri
        private void AddRecentFilterSet(string title, IReadOnlyList<string> origins, IReadOnlyList<string> materials, IReadOnlyList<string> sizes)
        {
            try
            {
                var btn = new Button { Content = title, Style = (Style)FindResource("ChipButtonStyle") };
                btn.Click += async (_, __) => { await ApplyAdvancedFiltersAsync(origins, materials, sizes); };
                RecentFiltersWrap.Children.Insert(0, btn);
                // En fazla 5 tut
                while (RecentFiltersWrap.Children.Count > 5) RecentFiltersWrap.Children.RemoveAt(RecentFiltersWrap.Children.Count - 1);
            }
            catch { }
        }

        private async void PriceRange_Changed(object sender, RoutedEventArgs e)
        {
            _minPrice = TryParseDecimal(PriceMinText.Text);
            _maxPrice = TryParseDecimal(PriceMaxText.Text);
            await LoadProductsAsync();
            await UpdateStatisticsAsync();
        }

        private async void PriceRange_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                await Dispatcher.InvokeAsync(() => PriceRange_Changed(sender, e));
            }
        }

        private void ToggleSummary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SummaryPanel.Visibility = SummaryPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
            catch { }
        }

        private void ToggleListOnly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Listeyi maksimuma çıkar: özet paneli gizle ve toolbar padding/margin'i minimize et
                if (SummaryPanel.Visibility == Visibility.Visible)
                {
                    SummaryPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch { }
        }

        private void ApplyImageDensity(bool large)
        {
            try
            {
                // Satır yüksekliği ve görsel kolon genişliği ayarı
                ProductsDataGrid.RowHeight = large ? 128 : 44;
                var imgCol = ProductsDataGrid.Columns.FirstOrDefault(c => (c.Header?.ToString() ?? "").Contains("Görsel"));
                if (imgCol != null)
                {
                    imgCol.Width = new DataGridLength(large ? 152 : 112);
                }
            }
            catch { }
        }

        private void ChkLargeImages_Checked(object sender, RoutedEventArgs e)
        {
            ApplyImageDensity(true);
        }

        private void ChkLargeImages_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplyImageDensity(false);
        }

        private void SaveFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var of = new
                {
                    OutOfStock = ChipOutOfStock.IsChecked == true,
                    LowStock = ChipLowStock.IsChecked == true,
                    Discounted = ChipDiscounted.IsChecked == true,
                    Min = PriceMinText.Text,
                    Max = PriceMaxText.Text,
                    Category = _currentCategoryFilter ?? "",
                    Search = (SearchTextBox.Text == "🔍 Ürün ara (ad, barkod, kategori)..." ? "" : SearchTextBox.Text)
                };
                var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "JSON|*.json", FileName = "filters.json" };
                if (sfd.ShowDialog() == true)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(of, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(sfd.FileName, json);
                    ShowToastNotification("Filtre profili kaydedildi", "success");
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Filtre kaydetme hatası: {ex.Message}", "error");
            }
        }

        private async void LoadFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "JSON|*.json" };
                if (ofd.ShowDialog() == true)
                {
                    var json = System.IO.File.ReadAllText(ofd.FileName);
                    var obj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    bool GetBool(string k) => obj.TryGetProperty(k, out var v) && v.GetBoolean();
                    string GetStr(string k) => obj.TryGetProperty(k, out var v) ? v.GetString() ?? "" : "";
                    ChipOutOfStock.IsChecked = GetBool("OutOfStock");
                    ChipLowStock.IsChecked = GetBool("LowStock");
                    ChipDiscounted.IsChecked = GetBool("Discounted");
                    PriceMinText.Text = GetStr("Min");
                    PriceMaxText.Text = GetStr("Max");
                    var cat = GetStr("Category");
                    _currentCategoryFilter = cat;
                    UpdateSelectedCategoryDisplay();
                    var search = GetStr("Search");
                    SearchTextBox.Text = string.IsNullOrWhiteSpace(search) ? "" : search;
                    await LoadProductsAsync();
                    await UpdateStatisticsAsync();
                    ShowToastNotification("Filtre profili yüklendi", "success");
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Filtre yükleme hatası: {ex.Message}", "error");
            }
        }

        private void ToggleCategorySidebar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CategorySidebarPanel.Visibility == Visibility.Visible)
                {
                    // Sidebar açıksa kapat
                    CategorySidebarPanel.Visibility = Visibility.Collapsed;
                    ShowToastNotification("Kategori paneli kapatıldı", "info");
                }
                else
                {
                    // Sidebar kapalıysa aç ve kategorileri yükle
                    CategorySidebarPanel.Visibility = Visibility.Visible;
                    _ = LoadSidebarCategoriesAsync();
                    ShowToastNotification("Kategori paneli açıldı", "success");
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Kategori paneli hatası: {ex.Message}", "error");
            }
        }



        private void OpenCategoryManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowToastNotification("Kategori yöneticisi açılıyor...", "info");
                var ownerWindow = Window.GetWindow(this);
                var dlg = new CategoryManagerDialog { Owner = ownerWindow };
                dlg.ShowInTaskbar = false;
                dlg.Topmost = true;
                dlg.Loaded += (_, __) => { try { dlg.Activate(); dlg.Focus(); } catch { } };
                dlg.ShowDialog();
                ShowToastNotification("Kategori yöneticisi kapandı", "success");
                // Kategorileri yeniden yükle, filtreyi koru
                _ = LoadCategoriesAsync();
                // Kullanıcı ProductsView içinden kategori düğmesiyle yeni kategori oluşturduysa, sidebar kategorilerini yeniden yükle
                _ = LoadSidebarCategoriesAsync();
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Kategori yöneticisi hatası: {ex.Message}", "error");
            }
        }

        private static decimal? TryParseDecimal(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (decimal.TryParse(s.Replace("₺", "").Trim(), out var d)) return d;
            return null;
        }

        private async Task UpdateDbStatusAsync()
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                if (sp == null) { DbStatusText.Text = "DB: fail"; return; }
                if (_usingDemoService)
                {
                    DbStatusText.Text = $"DB: DEMO · Gösterilen={_displayedProducts.Count}";
                    return;
                }
                var mediator = sp.GetRequiredService<IMediator>();
                var status = await mediator.Send(new GetProductDbStatusQuery());
                if (!status.IsConnected)
                {
                    DbStatusText.Text = "DB: fail";
                    return;
                }
                DbStatusText.Text = $"DB: OK · Aktif={status.ActiveCount} · Toplam={status.TotalCount} · Gösterilen={_displayedProducts.Count}";
            }
            catch { DbStatusText.Text = "DB: fail"; }
        }

        private async void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChipOutOfStock.IsChecked = false;
                ChipLowStock.IsChecked = false;
                ChipDiscounted.IsChecked = false;
                PriceMinText.Text = string.Empty;
                PriceMaxText.Text = string.Empty;

                // Kategori filtresini sıfırla
                _currentCategoryFilter = "";
                UpdateSelectedCategoryDisplay();
                SidebarActiveCategory.Text = "Tüm Kategoriler";

                _minPrice = _maxPrice = null;
                _currentSearchTerm = "";
                SearchTextBox.Text = "";
                SetSearchPlaceholder();
                await LoadProductsAsync();
                await UpdateStatisticsAsync();
                await UpdateDbStatusAsync();
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Filtre sıfırlama hatası: {ex.Message}", "error");
            }
        }

        private async void BulkImportImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wizard = new ImageMapWizard { Owner = Window.GetWindow(this) };
                wizard.ShowDialog();
                await LoadProductsAsync();
                await UpdateStatisticsAsync();
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Görsel eşleme hatası: {ex.Message}", "error");
            }
        }

        // Görsel büyük önizleme
        private void OpenImageViewerForRow(ProductItem item)
        {
            try
            {
                var images = new List<System.Windows.Media.Imaging.BitmapImage>();
                foreach (var url in item.GalleryUrls)
                {
                    try
                    {
                        var src = new System.Windows.Media.Imaging.BitmapImage();
                        src.BeginInit();
                        src.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        src.UriSource = new Uri(url, UriKind.RelativeOrAbsolute);
                        src.EndInit();
                        src.Freeze();
                        images.Add(src);
                    }
                    catch { }
                }
                if (images.Count == 0 && item.ImageSource != null) images.Add(item.ImageSource);
                if (images.Count == 0) return;
                var viewer = new ProductImageViewer(images) { Owner = Application.Current.MainWindow };
                viewer.ShowDialog();
            }
            catch { }
        }

        private string GetCategoryIcon(string category)
        {
            return category.ToLower() switch
            {
                "elektronik" => "⚡",
                "içecek" => "🥤",
                "atıştırmalık" => "🍿",
                "kozmetik" => "💄",
                "spor" => "⚽",
                "gıda" => "🍎",
                "oyuncak" => "🧸",
                "ev gereçleri" => "🏠",
                "kırtasiye" => "✏️",
                "sağlık" => "💊",
                _ => "📦"
            };
        }

        private void SetSearchPlaceholder()
        {
            if (string.IsNullOrEmpty(SearchTextBox.Text))
            {
                SearchTextBox.Text = "🔍 Ürün ara (ad, barkod, kategori)...";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private System.Threading.CancellationTokenSource? _searchCts;
        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text == "🔍 Ürün ara (ad, barkod, kategori)...")
                return;

            _currentSearchTerm = SearchTextBox.Text?.Trim() ?? "";
            try
            {
                _searchCts?.Cancel();
                _searchCts = new System.Threading.CancellationTokenSource();
                var token = _searchCts.Token;
                await Task.Delay(250, token);
                await LoadProductsAsync();
                await UpdateStatisticsAsync();
            }
            catch (TaskCanceledException) { }
            catch { }
        }

        // CategoryFilter_Changed kaldırıldı - artık sidebar kullanılıyor

        private async Task UpdateStatisticsAsync()
        {
            try
            {
                var statistics = await _productService.GetStatisticsAsync();

                TotalProductsText.Text = statistics.TotalProducts.ToString();
                TotalValueText.Text = $"₺{statistics.TotalValue:N0}";
                LowStockText.Text = statistics.LowStockCount.ToString();
                CriticalStockText.Text = $"{statistics.CriticalStockCount} ürün";
                AveragePriceText.Text = $"₺{statistics.AveragePrice:F2}";
            }
            catch (Exception ex)
            {
                ShowToastNotification($"İstatistik güncelleme hatası: {ex.Message}", "error");
            }
        }

        // Pagination Event Handlers
        private async void ProductPagination_PageChanged(object sender, PaginationEventArgs e)
        {
            await LoadProductsAsync();
        }

        private async void ProductPagination_PageSizeChanged(object sender, PaginationEventArgs e)
        {
            await LoadProductsAsync();
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

        // Inline fiyat/stok düzenlemeleri
        private async void PriceTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            await CommitInlineEdit(sender as TextBox, isPrice: true);
        }

        private async void PriceTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) { await CommitInlineEdit(sender as TextBox, isPrice: true); }
        }

        private async void StockTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            await CommitInlineEdit(sender as TextBox, isPrice: false);
        }

        private async void StockTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) { await CommitInlineEdit(sender as TextBox, isPrice: false); }
        }

        private async Task CommitInlineEdit(TextBox? tb, bool isPrice)
        {
            if (tb == null) return;
            if (tb.DataContext is not ProductItem row) return;
            try
            {
                if (isPrice)
                {
                    if (!decimal.TryParse(tb.Text?.Trim(), out var val)) { ShowToastNotification("Geçersiz değer", "error"); MarkInvalid(tb, "Geçersiz değer"); return; }
                    var updated = row.Clone();
                    // Hangi alan? TextBox Binding.Path'i ile karar verelim
                    var binding = tb.GetBindingExpression(TextBox.TextProperty)?.ParentBinding;
                    var path = binding?.Path?.Path ?? "";
                    bool ok;
                    if (path.Contains("SalePrice"))
                    {
                        if (val < 0) { MarkInvalid(tb, "Satış fiyatı negatif olamaz"); return; }
                        updated.Price = val;
                        ok = await _productService.UpdateFinanceAsync(row.Id, salePrice: val);
                    }
                    else if (path.Contains("PurchasePrice"))
                    {
                        if (val < 0) { MarkInvalid(tb, "Alış fiyatı negatif olamaz"); return; }
                        ok = await _productService.UpdateFinanceAsync(row.Id, purchasePrice: val);
                    }
                    else if (path.Contains("DiscountRate"))
                    {
                        if (val < 0 || val > 100) { MarkInvalid(tb, "%İskonto 0-100 aralığında olmalı"); return; }
                        ok = await _productService.UpdateFinanceAsync(row.Id, discountRate: val);
                    }
                    else
                    {
                        ok = await _productService.UpdateProductAsync(updated);
                    }
                    if (!ok) ShowToastNotification("Fiyat güncellenemedi", "error"); else ClearInvalid(tb);
                }
                else
                {
                    if (!int.TryParse(tb.Text?.Trim(), out var val)) { ShowToastNotification("Geçersiz stok", "error"); MarkInvalid(tb, "Geçersiz stok"); return; }
                    if (val < 0) { MarkInvalid(tb, "Stok negatif olamaz"); return; }
                    var ok = await _productService.UpdateStockAsync(row.Id, val);
                    if (!ok) ShowToastNotification("Stok güncellenemedi", "error"); else ClearInvalid(tb);
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Inline kayıt hatası: {ex.Message}", "error");
            }
            finally
            {
                await LoadProductsAsync();
                await UpdateStatisticsAsync();
            }
        }

        // Satır işlemleri
        private void RowActions_Open(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.ContextMenu != null)
            {
                b.ContextMenu.DataContext = b.DataContext;
                b.ContextMenu.IsOpen = true;
            }
        }

        private void RowEdit_Click(object sender, RoutedEventArgs e)
        {
            _selectedProduct = (sender as FrameworkElement)?.DataContext as ProductItem;
            EditProduct_Click(sender, e);
        }
        private void RowUpdateStock_Click(object sender, RoutedEventArgs e)
        {
            _selectedProduct = (sender as FrameworkElement)?.DataContext as ProductItem;
            UpdateStock_Click(sender, e);
        }
        private void RowUpdatePrice_Click(object sender, RoutedEventArgs e)
        {
            _selectedProduct = (sender as FrameworkElement)?.DataContext as ProductItem;
            UpdatePrice_Click(sender, e);
        }
        private void RowDelete_Click(object sender, RoutedEventArgs e)
        {
            _selectedProduct = (sender as FrameworkElement)?.DataContext as ProductItem;
            DeleteProduct_Click(sender, e);
        }

        private void RowOpenImage_Click(object sender, RoutedEventArgs e)
        {
            _selectedProduct = (sender as FrameworkElement)?.DataContext as ProductItem;
            if (_selectedProduct != null) OpenImageViewerForRow(_selectedProduct);
        }

        private void RowCopyBarcode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = (sender as FrameworkElement)?.DataContext as ProductItem ?? _selectedProduct;
                if (item == null) return;
                Clipboard.SetText(item.Barcode ?? string.Empty);
                ShowToastNotification("Barkod kopyalandı", "success");
            }
            catch { }
        }
        private void RowCopySku_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = (sender as FrameworkElement)?.DataContext as ProductItem ?? _selectedProduct;
                if (item == null) return;
                Clipboard.SetText(item.Sku ?? string.Empty);
                ShowToastNotification("SKU kopyalandı", "success");
            }
            catch { }
        }
        private void RowCopyName_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = (sender as FrameworkElement)?.DataContext as ProductItem ?? _selectedProduct;
                if (item == null) return;
                Clipboard.SetText(item.Name ?? string.Empty);
                ShowToastNotification("Ad kopyalandı", "success");
            }
            catch { }
        }

        private void RowOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = (sender as FrameworkElement)?.DataContext as ProductItem;
                if (item == null) return;
                var storage = App.ServiceProvider?.GetService<ImageStorageService>() ?? new ImageStorageService();
                var folder = storage.GetProductFolder(item.Id);
                if (System.IO.Directory.Exists(folder)) Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
            }
            catch { }
        }

        private void ShowSelectedProductDetails(ProductItem product)
        {
            SelectedProductName.Text = product.Name;
            SelectedProductBarcode.Text = $"Barkod: {product.Barcode}";
            SelectedProductCategory.Text = $"Kategori: {product.Category}";
            SelectedProductPrice.Text = $"Fiyat: ₺{product.SalePrice:F2}";
            SelectedProductStock.Text = $"Stok: {product.Stock} adet";
            SelectedProductNet.Text = $"Net: ₺{product.FinalPrice:F2}";
            SelectedProductMargin.Text = $"Marj: ₺{product.MarginAmount:F2} (%{product.MarginPercent:F2})";
        }

        private void ThumbCell_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as System.Windows.Controls.Image)?.DataContext is ProductItem item)
            {
                try
                {
                    // Çift tık: büyük galeri penceresi
                    if (e.ClickCount >= 2)
                    {
                        OpenImageViewerForRow(item);
                        e.Handled = true;
                        return;
                    }

                    // Tek tık: aynı satırın detayını aç/kapat (toggle)
                    if (ProductsDataGrid.SelectedItem == item)
                    {
                        ProductsDataGrid.SelectedItem = null; // daralt
                    }
                    else
                    {
                        ProductsDataGrid.SelectedItem = item; // genişlet
                        ProductsDataGrid.ScrollIntoView(item);
                    }
                    e.Handled = true;
                }
                catch { }
            }
        }

        // Event Handlers
        private async void RefreshProducts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadCategoriesAsync();
                await LoadProductsAsync();
                await UpdateStatisticsAsync();
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo("[UI] Ürün listesi yenilendi", nameof(ProductsView));
                ShowToastNotification("✅ Ürün listesi yenilendi!", "success");
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Yenileme hatası: {ex.Message}", "error");
            }
        }

        // Barkod dinleme – ProductsView seviyesinde
        private async void ChkBarcodeListen_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var svc = sp?.GetService<MesTechStok.Desktop.Services.IBarcodeService>();
                if (svc == null) { ShowToastNotification("Barkod servisi yok", "warning"); return; }
                svc.BarcodeScanned += BarcodeSvcOnBarcodeScanned;
                try { svc.DeviceStatusChanged += SvcOnDeviceStatusChanged; } catch { }
                if (!svc.IsConnected) await svc.ConnectAsync();
                await svc.StartScanningAsync();
                _barcodeListening = true;
                ShowToastNotification("Barkod dinleme aktif", "info");
                try { BarcodeStatusDot.Fill = System.Windows.Media.Brushes.LimeGreen; BarcodeStatusText.Text = "Aktif"; } catch { }
            }
            catch { }
        }

        private async void ChkBarcodeListen_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var svc = sp?.GetService<MesTechStok.Desktop.Services.IBarcodeService>();
                if (svc == null) return;
                await svc.StopScanningAsync();
                svc.BarcodeScanned -= BarcodeSvcOnBarcodeScanned;
                try { svc.DeviceStatusChanged -= SvcOnDeviceStatusChanged; } catch { }
                await svc.DisconnectAsync();
                _barcodeListening = false;
                ShowToastNotification("Barkod dinleme kapalı", "info");
                try { BarcodeStatusDot.Fill = System.Windows.Media.Brushes.Gray; BarcodeStatusText.Text = "Kapalı"; } catch { }
            }
            catch { }
        }

        private void SvcOnDeviceStatusChanged(object? sender, string e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (string.Equals(e, "connected", StringComparison.OrdinalIgnoreCase))
                    {
                        BarcodeStatusDot.Fill = System.Windows.Media.Brushes.LimeGreen;
                        BarcodeStatusText.Text = "Bağlı";
                    }
                    else if (string.Equals(e, "scanning", StringComparison.OrdinalIgnoreCase))
                    {
                        BarcodeStatusDot.Fill = System.Windows.Media.Brushes.Gold;
                        BarcodeStatusText.Text = "Taranıyor";
                    }
                    else
                    {
                        BarcodeStatusDot.Fill = System.Windows.Media.Brushes.Gray;
                        BarcodeStatusText.Text = "Kapalı";
                    }
                });
            }
            catch { }
        }

        private async void BarcodeSvcOnBarcodeScanned(object? sender, BarcodeScannedEventArgs e)
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    LblLastBarcodePV.Text = e.Barcode;
                    try { BarcodeStatusDot.Fill = System.Windows.Media.Brushes.Gold; BarcodeStatusText.Text = "Okundu"; } catch { }
                    var mode = (ScanModeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Listeyi Filtrele";
                    if (mode.Contains("Listeyi Filtrele"))
                    {
                        SearchTextBox.Text = e.Barcode;
                        _currentSearchTerm = e.Barcode;
                        await LoadProductsAsync();
                        await UpdateStatisticsAsync();
                    }
                    else // Yeni Ürün Aç
                    {
                        ProductUploadWindowManager.TryOpenWithBarcode(Window.GetWindow(this), e.Barcode);
                    }
                    try { await Task.Delay(600); BarcodeStatusDot.Fill = System.Windows.Media.Brushes.LimeGreen; BarcodeStatusText.Text = "Aktif"; } catch { }
                });
            }
            catch { }
        }

        private async void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
                // Şu anda tüm kullanıcılar ürün ekleyebilir
                // Yeni ürün için popup (boş form)
                ProductUploadWindowManager.TryOpen(Window.GetWindow(this));
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Ürün ekleme hatası: {ex.Message}", "error");
            }
        }

        private void OpenProductUploadPopup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ok = ProductUploadWindowManager.TryOpen(Window.GetWindow(this));
                if (ok) ShowToastNotification("🧾 Ürün Yükleme popup açıldı", "success");
                else ShowToastNotification("❗ Ürün Yükleme popup açılamadı (limit dolu)", "warning");
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Popup açılamadı: {ex.Message}", "error");
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogError($"Popup açılamadı: {ex}", nameof(ProductsView));
            }
        }

        private async void AddWithBarcode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var barcodeDialog = new BarcodeIntegrationDialog();
                if (barcodeDialog.ShowDialog() == true && !string.IsNullOrEmpty(barcodeDialog.ScannedBarcode))
                {
                    // Barkod ön-dolu yeni ürün popup
                    ProductUploadWindowManager.TryOpenWithBarcode(Window.GetWindow(this), barcodeDialog.ScannedBarcode);
                }
                // Kamera/okuyucu güvenli kapatma
                try
                {
                    var svc = MesTechStok.Desktop.App.ServiceProvider?.GetService<MesTechStok.Desktop.Services.IBarcodeService>();
                    if (svc != null)
                    {
                        await svc.StopScanningAsync();
                        await svc.DisconnectAsync();
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Barkodlu ürün ekleme hatası: {ex.Message}", "error");
            }
        }

        private async void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null) return;

            try
            {
                // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
                // Şu anda tüm kullanıcılar ürün düzenleyebilir
                // Güncel veriyi DB'den alıp popup'a aktar ki görseller en güncel haliyle gelsin
                var refreshed = await _productService.GetProductByIdAsync(_selectedProduct.Id) ?? _selectedProduct;
                ProductUploadWindowManager.TryOpen(Window.GetWindow(this), refreshed);
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Ürün düzenleme hatası: {ex.Message}", "error");
            }
        }

        private async void UpdateStock_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null) return;

            try
            {
                // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
                // Şu anda tüm kullanıcılar stok güncelleyebilir
                var dialog = new StockUpdateDialog(_selectedProduct);
                if (dialog.ShowDialog() == true)
                {
                    var success = await _productService.UpdateStockAsync(_selectedProduct.Id, dialog.NewStock);
                    if (success)
                    {
                        await LoadProductsAsync();
                        await UpdateStatisticsAsync();
                        MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo($"[STOCK] Stok güncellendi: Id={_selectedProduct.Id} Yeni={dialog.NewStock}", nameof(ProductsView));
                        ShowToastNotification($"✅ Stok güncellendi!\n{_selectedProduct.Name}\nYeni Stok: {dialog.NewStock} adet", "success");
                    }
                    else
                    {
                        ShowToastNotification("❌ Stok güncellenemedi!", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Stok güncelleme hatası: {ex.Message}", "error");
            }
        }

        private async void UpdatePrice_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null) return;

            try
            {
                // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
                // Şu anda tüm kullanıcılar fiyat güncelleyebilir
                var dialog = new PriceUpdateDialog(_selectedProduct);
                if (dialog.ShowDialog() == true)
                {
                    var updatedProduct = _selectedProduct.Clone();
                    updatedProduct.Price = dialog.NewPrice;

                    var success = await _productService.UpdateProductAsync(updatedProduct);
                    if (success)
                    {
                        await LoadProductsAsync();
                        await UpdateStatisticsAsync();
                        MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo($"[PRICE] Fiyat güncellendi: Id={_selectedProduct.Id} Yeni=₺{dialog.NewPrice:F2}", nameof(ProductsView));
                        ShowToastNotification($"✅ Fiyat güncellendi!\n{_selectedProduct.Name}\nYeni Fiyat: ₺{dialog.NewPrice:F2}", "success");
                    }
                    else
                    {
                        ShowToastNotification("❌ Fiyat güncellenemedi!", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Fiyat güncelleme hatası: {ex.Message}", "error");
            }
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null) return;

            try
            {
                // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
                // Şu anda tüm kullanıcılar ürün silebilir
                var result = MessageBox.Show($"'{_selectedProduct.Name}' ürününü silmek istediğinizden emin misiniz?\n\nBu işlem geri alınamaz!",
                    "Ürün Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var productName = _selectedProduct.Name;
                    var success = await _productService.DeleteProductAsync(_selectedProduct.Id);
                    if (success)
                    {
                        await LoadProductsAsync();
                        await UpdateStatisticsAsync();

                        SelectedProductPanel.Visibility = Visibility.Collapsed;
                        ProductDetailsPanel.Visibility = Visibility.Collapsed;

                        ShowToastNotification($"✅ Ürün silindi!\n{productName}", "success");
                    }
                    else
                    {
                        ShowToastNotification("❌ Ürün silinemedi!", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Ürün silme hatası: {ex.Message}", "error");
            }
        }

        private async void ForceSeed_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                if (sp == null) { ShowToastNotification("Servis bulunamadı", "error"); return; }
                var mediator = sp.GetRequiredService<IMediator>();
                var seedResult = await mediator.Send(new SeedDemoDataCommand());
                if (!seedResult.IsSuccess)
                {
                    ShowToastNotification($"Demo yükleme hatası: {seedResult.Message}", "error");
                    return;
                }
                if (seedResult.WasSkipped)
                {
                    ShowToastNotification(seedResult.Message ?? "Atlandı.", "info");
                    return;
                }
                ShowToastNotification("✅ Demo verileri yüklendi.", "success");
                // DB'ye döneriz
                _productService = App.ServiceProvider?.GetService<IProductDataService>() ?? _productService;
                _usingDemoService = false;
                await LoadCategoriesAsync();
                await LoadProductsAsync();
                await UpdateStatisticsAsync();
                await UpdateDbStatusAsync();
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Demo yükleme hatası: {ex.Message}", "error");
            }
        }

        private async void ForceCreate40_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                if (sp == null) { ShowToastNotification("Servis bulunamadı", "error"); return; }
                var mediator = sp.GetRequiredService<IMediator>();
                var bulkResult = await mediator.Send(new CreateBulkProductsCommand(40));
                if (!bulkResult.IsSuccess)
                {
                    ShowToastNotification($"Manuel ürün ekleme hatası: {bulkResult.Message}", "error");
                    return;
                }

                var created = bulkResult.CreatedCount;
                _productService = App.ServiceProvider?.GetService<IProductDataService>() ?? _productService;
                _usingDemoService = false;
                await LoadCategoriesAsync();
                await LoadProductsAsync();
                await UpdateStatisticsAsync();
                await UpdateDbStatusAsync();
                ShowToastNotification($"✅ Manuel {created} ürün eklendi", "success");
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Manuel ürün ekleme hatası: {ex.Message}", "error");
            }
        }

        private void ImportFromExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wizard = new ProductImportWizard { Owner = Window.GetWindow(this) };
                wizard.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Excel içe aktarma hatası: {ex.Message}", "error");
            }
        }

        // Numeric input guards
        private void Decimal_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9.,]+$");
        }
        private void Integer_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Excel dosyası kaydet",
                    Filter = "Excel Files|*.xlsx|All Files|*.*",
                    FileName = $"Urunler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };
                if (dialog.ShowDialog() == true)
                {
                    if (_displayedProducts == null || _displayedProducts.Count == 0)
                    {
                        ShowToastNotification("Dışa aktarılacak ürün yok", "info");
                        return;
                    }
                    using (var wb = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = wb.AddWorksheet("Ürünler");
                        int r = 1;
                        string[] headers = new[] {
                            "Id", "SKU", "Barkod", "Ad", "Kategori",
                            "Menşei", "Materyal", "Hacim", "Bedenler",
                            "Boy(cm)", "En(cm)", "Yükseklik(cm)",
                            "Desi", "Termin(gün)", "Sevkiyat Adresi", "İade Adresi",
                            "Kapak Görsel", "Ek Görseller",
                            "Alış", "Satış", "%İskonto", "Net", "Stok", "MinStok", "Oluşturma", "Güncelleme"
                        };
                        for (int c = 0; c < headers.Length; c++) ws.Cell(r, c + 1).Value = headers[c];
                        ws.Row(r).Style.Font.Bold = true;
                        foreach (var p in _displayedProducts)
                        {
                            r++;
                            int c = 1;
                            ws.Cell(r, c++).Value = p.Id.ToString();
                            ws.Cell(r, c++).Value = p.Sku;
                            ws.Cell(r, c++).Value = p.Barcode;
                            ws.Cell(r, c++).Value = p.Name;
                            ws.Cell(r, c++).Value = p.Category;
                            ws.Cell(r, c++).Value = p.Origin;
                            ws.Cell(r, c++).Value = p.Material;
                            ws.Cell(r, c++).Value = p.VolumeText;
                            ws.Cell(r, c++).Value = p.Sizes;
                            ws.Cell(r, c++).Value = p.LengthCm;
                            ws.Cell(r, c++).Value = p.WidthCm;
                            ws.Cell(r, c++).Value = p.HeightCm;
                            ws.Cell(r, c++).Value = p.Desi;
                            ws.Cell(r, c++).Value = p.LeadTimeDays;
                            ws.Cell(r, c++).Value = p.ShipAddress;
                            ws.Cell(r, c++).Value = p.ReturnAddress;
                            ws.Cell(r, c++).Value = p.ImageUrl;
                            ws.Cell(r, c++).Value = p.AdditionalImageUrls;
                            ws.Cell(r, c++).Value = p.PurchasePrice;
                            ws.Cell(r, c++).Value = p.SalePrice;
                            ws.Cell(r, c++).Value = p.DiscountRate;
                            ws.Cell(r, c++).Value = p.FinalPrice;
                            ws.Cell(r, c++).Value = p.Stock;
                            ws.Cell(r, c++).Value = p.MinimumStock;
                            ws.Cell(r, c++).Value = p.CreatedDate;
                            ws.Cell(r, c++).Value = p.LastUpdated;
                        }
                        ws.Columns().AdjustToContents();
                        wb.SaveAs(dialog.FileName);
                    }
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo($"[EXPORT] Excel oluşturuldu: {System.IO.Path.GetFileName(dialog.FileName)}", nameof(ProductsView));
                    ShowToastNotification("✅ Excel dışa aktarıldı", "success");
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Excel dışa aktarma hatası: {ex.Message}", "error");
            }
        }



        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "🔍 Ürün ara (ad, barkod, kategori)...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SetSearchPlaceholder();
            }
        }

        private void ShowToastNotification(string message, string type)
        {
            // MainWindow'daki toast sistemi ile entegre et
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ShowToastNotification(message, type);
            }
        }

        private void ProductsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is ProductItem item)
            {
                try
                {
                    ProductUploadWindowManager.TryOpen(Window.GetWindow(this), item);
                }
                catch
                {
                    OpenImageViewerForRow(item);
                }
            }
        }

        private void SelectedOpenImage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct != null)
            {
                OpenImageViewerForRow(_selectedProduct);
            }
        }

        private void SelectedOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedProduct == null) return;
                var storage = App.ServiceProvider?.GetService<ImageStorageService>() ?? new ImageStorageService();
                var folder = storage.GetProductFolder(_selectedProduct.Id);
                if (System.IO.Directory.Exists(folder)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = folder, UseShellExecute = true });
            }
            catch { }
        }

        private void ProductsDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // Çoklu sıralama için Shift basılıysa mevcut sıralamaya ekle, değilse normal davranış
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true;
                var col = e.Column;
                var dir = col.SortDirection != System.ComponentModel.ListSortDirection.Ascending
                    ? System.ComponentModel.ListSortDirection.Ascending
                    : System.ComponentModel.ListSortDirection.Descending;
                col.SortDirection = dir;
                // UI koleksiyonunu sırayla yeniden düzenlemek yerine, servis çağrısına çoklu sort aktarımı ileride yapılacak.
                // Şimdilik tek sütun sıralama davranışını koruyoruz (görsel ipucu için).
            }
        }

        private void ColumnsMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Popup içeriğini her açılışta tazele
                if (ColumnsPopupPanel != null)
                {
                    ColumnsPopupPanel.Children.Clear();
                    foreach (var col in ProductsDataGrid.Columns)
                    {
                        var cb = new CheckBox { Content = col.Header?.ToString() ?? string.Empty, IsChecked = col.Visibility == Visibility.Visible, Margin = new Thickness(0, 0, 12, 8) };
                        cb.Checked += (_, __) => col.Visibility = Visibility.Visible;
                        cb.Unchecked += (_, __) => col.Visibility = Visibility.Collapsed;
                        ColumnsPopupPanel.Children.Add(cb);
                    }
                }
                ColumnsPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Kolon yöneticisi hatası: {ex.Message}", "error");
            }
        }

        // Kolon arama ve hızlı seçim
        private void ColumnsSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var term = (sender as TextBox)?.Text?.Trim() ?? string.Empty;
                foreach (var child in ColumnsPopupPanel.Children.OfType<CheckBox>())
                {
                    var text = child.Content?.ToString() ?? string.Empty;
                    child.Visibility = string.IsNullOrWhiteSpace(term) || text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
            catch { }
        }

        private void ColumnsSelectAll_Click(object sender, RoutedEventArgs e)
        {
            try { foreach (var cb in ColumnsPopupPanel.Children.OfType<CheckBox>()) cb.IsChecked = true; } catch { }
        }
        private void ColumnsSelectNone_Click(object sender, RoutedEventArgs e)
        {
            try { foreach (var cb in ColumnsPopupPanel.Children.OfType<CheckBox>()) cb.IsChecked = false; } catch { }
        }

        private void ColumnsPopup_Save_Click(object sender, RoutedEventArgs e)
        {
            // Profil seçimli hızlı kaydet (isim soran hızlı kaydet butonu zaten var)
            SaveColumnsProfile_Click(sender, e);
        }

        private void ColumnsPopup_Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetColumnsToDefault();
        }

        private void ResetColumnsToDefault()
        {
            try
            {
                foreach (var c in ProductsDataGrid.Columns)
                {
                    c.Visibility = Visibility.Visible;
                }
                // Basit varsayılan sıra: mevcut DisplayIndex’e göre yeniden sırala
                int idx = 0;
                foreach (var c in ProductsDataGrid.Columns.OrderBy(c => c.DisplayIndex))
                {
                    c.DisplayIndex = idx++;
                }
                ShowToastNotification("Kolonlar varsayılana alındı", "success");
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Varsayılana alma hatası: {ex.Message}", "error");
            }
        }

        private static void MarkInvalid(TextBox tb, string message)
        {
            try { tb.ToolTip = message; tb.BorderBrush = System.Windows.Media.Brushes.Red; tb.BorderThickness = new Thickness(2); } catch { }
        }
        private static void ClearInvalid(TextBox tb)
        {
            try { tb.ToolTip = null; tb.ClearValue(TextBox.BorderBrushProperty); tb.ClearValue(TextBox.BorderThicknessProperty); } catch { }
        }

        private void SaveColumnsProfile_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var columns = ProductsDataGrid.Columns
                    .Select(c => new ColumnProfile
                    {
                        Header = c.Header?.ToString() ?? string.Empty,
                        Visible = c.Visibility == Visibility.Visible,
                        DisplayIndex = c.DisplayIndex,
                        Width = c.ActualWidth > 0 ? c.ActualWidth : (c.Width.IsAbsolute ? c.Width.Value : 120)
                    })
                    .ToList();
                // İsimli profil kaydı
                var input = Microsoft.VisualBasic.Interaction.InputBox("Profil adı:", "Kolon Profilini Kaydet", "Varsayilan");
                var name = string.IsNullOrWhiteSpace(input) ? $"columns_{DateTime.Now:yyyyMMdd_HHmmss}" : input.Trim();
                var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "JSON|*.json", FileName = name + ".json" };
                if (sfd.ShowDialog() == true)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(columns, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(sfd.FileName, json);
                    ShowToastNotification($"Kolon profili kaydedildi: {System.IO.Path.GetFileNameWithoutExtension(sfd.FileName)}", "success");
                    // Otomatik uygula bayrağı
                    _pendingAutoSaveColumns = columns;
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Kolon profili kaydetme hatası: {ex.Message}", "error");
            }
        }

        // Kolon değişikliklerinde otomatik profil dosyasını (Autosave.json) güncel tut
        private List<ColumnProfile>? _pendingAutoSaveColumns;
        private void ProductsDataGrid_ColumnChanged(object? sender, EventArgs e)
        {
            try
            {
                var columns = ProductsDataGrid.Columns
                    .Select(c => new ColumnProfile
                    {
                        Header = c.Header?.ToString() ?? string.Empty,
                        Visible = c.Visibility == Visibility.Visible,
                        DisplayIndex = c.DisplayIndex,
                        Width = c.ActualWidth > 0 ? c.ActualWidth : (c.Width.IsAbsolute ? c.Width.Value : 120)
                    })
                    .ToList();
                EnsureProfilesDir();
                var path = System.IO.Path.Combine(_profilesDir, "Autosave.json");
                var json = System.Text.Json.JsonSerializer.Serialize(columns, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(path, json);
            }
            catch { }
        }

        private void LoadColumnsProfile_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "JSON|*.json" };
                if (ofd.ShowDialog() == true)
                {
                    var json = System.IO.File.ReadAllText(ofd.FileName);
                    var profiles = System.Text.Json.JsonSerializer.Deserialize<List<ColumnProfile>>(json) ?? new List<ColumnProfile>();
                    var byHeader = ProductsDataGrid.Columns.ToDictionary(c => c.Header?.ToString() ?? string.Empty, c => c);
                    // Apply visibility first
                    foreach (var p in profiles)
                    {
                        if (byHeader.TryGetValue(p.Header, out var col))
                        {
                            col.Visibility = p.Visible ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                    // Apply order
                    foreach (var p in profiles.OrderBy(p => p.DisplayIndex))
                    {
                        if (byHeader.TryGetValue(p.Header, out var col))
                        {
                            col.DisplayIndex = Math.Max(0, Math.Min(p.DisplayIndex, ProductsDataGrid.Columns.Count - 1));
                        }
                    }
                    // Apply widths
                    foreach (var p in profiles)
                    {
                        if (byHeader.TryGetValue(p.Header, out var col))
                        {
                            if (p.Width > 0) col.Width = new DataGridLength(p.Width);
                        }
                    }
                    ShowToastNotification("Kolon profili yüklendi", "success");
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Kolon profili yükleme hatası: {ex.Message}", "error");
            }
        }

        private class ColumnProfile
        {
            public string Header { get; set; } = string.Empty;
            public bool Visible { get; set; }
            public int DisplayIndex { get; set; }
            public double Width { get; set; }
        }

        // Bridge DTO for ReportsView
        public class ColumnProfileDto
        {
            public string Header { get; set; } = string.Empty;
            public bool Visible { get; set; }
            public int DisplayIndex { get; set; }
            public double Width { get; set; }
        }

        // Basit static köprü: ReportsView bu API üzerinden ProductsView kolon profilini alıp uygular
        public static class ProductsViewProfilesBridge
        {
            private static ProductsView? _lastInstance;

            public static void Register(ProductsView instance)
            {
                _lastInstance = instance;
            }

            public static List<ColumnProfileDto> CaptureProfiles()
            {
                var v = _lastInstance;
                if (v == null) return new List<ColumnProfileDto>();
                return v.ProductsDataGrid.Columns.Select(c => new ColumnProfileDto
                {
                    Header = c.Header?.ToString() ?? string.Empty,
                    Visible = c.Visibility == Visibility.Visible,
                    DisplayIndex = c.DisplayIndex,
                    Width = c.ActualWidth > 0 ? c.ActualWidth : (c.Width.IsAbsolute ? c.Width.Value : 120)
                }).ToList();
            }

            public static void ApplyProfiles(List<ColumnProfileDto> profiles)
            {
                var v = _lastInstance;
                if (v == null || profiles == null || profiles.Count == 0) return;
                v.ApplyColumnProfiles(profiles.Select(p => new ColumnProfile
                {
                    Header = p.Header,
                    Visible = p.Visible,
                    DisplayIndex = p.DisplayIndex,
                    Width = p.Width
                }).ToList());
            }

            public static void ResetToDefault()
            {
                var v = _lastInstance;
                v?.ResetColumnsToDefault();
            }
        }

        // Hızlı kolon profili seçimi: docs klasörüne json dosyaları olarak kaydet/yükle
        private string _profilesDir => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
        private void EnsureProfilesDir()
        {
            try { System.IO.Directory.CreateDirectory(_profilesDir); } catch { }
        }

        private void ColumnProfilesCombo_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                EnsureProfilesDir();
                var files = System.IO.Directory.GetFiles(_profilesDir, "*.json");
                ColumnProfilesCombo.Items.Clear();
                ColumnProfilesCombo.Items.Add(new ComboBoxItem { Content = "Kolon Profili Seç", IsSelected = true });
                foreach (var f in files.OrderBy(x => x))
                {
                    ColumnProfilesCombo.Items.Add(new ComboBoxItem { Content = System.IO.Path.GetFileNameWithoutExtension(f), Tag = f });
                }
            }
            catch { }
        }

        private void ColumnProfilesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ColumnProfilesCombo.SelectedItem is ComboBoxItem it && it.Tag is string path && System.IO.File.Exists(path))
                {
                    var json = System.IO.File.ReadAllText(path);
                    var profiles = System.Text.Json.JsonSerializer.Deserialize<List<ColumnProfile>>(json) ?? new List<ColumnProfile>();
                    ApplyColumnProfiles(profiles);
                    ShowToastNotification($"Kolon profili yüklendi: {System.IO.Path.GetFileNameWithoutExtension(path)}", "success");
                }
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Profil yükleme hatası: {ex.Message}", "error");
            }
        }

        private void ApplyColumnProfiles(List<ColumnProfile> profiles)
        {
            var byHeader = ProductsDataGrid.Columns.ToDictionary(c => c.Header?.ToString() ?? string.Empty, c => c);
            foreach (var p in profiles)
            {
                if (byHeader.TryGetValue(p.Header, out var col))
                {
                    col.Visibility = p.Visible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            foreach (var p in profiles.OrderBy(p => p.DisplayIndex))
            {
                if (byHeader.TryGetValue(p.Header, out var col))
                {
                    col.DisplayIndex = Math.Max(0, Math.Min(p.DisplayIndex, ProductsDataGrid.Columns.Count - 1));
                }
            }
            foreach (var p in profiles)
            {
                if (byHeader.TryGetValue(p.Header, out var col))
                {
                    if (p.Width > 0) col.Width = new DataGridLength(p.Width);
                }
            }
        }

        private void TryLoadAutosavedColumns()
        {
            try
            {
                EnsureProfilesDir();
                var path = System.IO.Path.Combine(_profilesDir, "Autosave.json");
                if (System.IO.File.Exists(path))
                {
                    var json = System.IO.File.ReadAllText(path);
                    var profiles = System.Text.Json.JsonSerializer.Deserialize<List<ColumnProfile>>(json) ?? new List<ColumnProfile>();
                    if (profiles.Count > 0)
                    {
                        ApplyColumnProfiles(profiles);
                        ShowToastNotification("Kolon düzeni geri yüklendi", "info");
                    }
                }
            }
            catch { }
        }

        private void SaveColumnsProfileQuick_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureProfilesDir();
                var columns = ProductsDataGrid.Columns
                    .Select(c => new ColumnProfile
                    {
                        Header = c.Header?.ToString() ?? string.Empty,
                        Visible = c.Visibility == Visibility.Visible,
                        DisplayIndex = c.DisplayIndex,
                        Width = c.ActualWidth > 0 ? c.ActualWidth : (c.Width.IsAbsolute ? c.Width.Value : 120)
                    })
                    .ToList();
                var input = Microsoft.VisualBasic.Interaction.InputBox("Profil adı:", "Kolon Profilini Kaydet", "Varsayilan");
                var name = string.IsNullOrWhiteSpace(input) ? $"columns_{DateTime.Now:yyyyMMdd_HHmmss}" : input.Trim();
                var path = System.IO.Path.Combine(_profilesDir, name + ".json");
                var json = System.Text.Json.JsonSerializer.Serialize(columns, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(path, json);
                ShowToastNotification($"Kolon profili kaydedildi: {name}", "success");
            }
            catch (Exception ex)
            {
                ShowToastNotification($"Hızlı profil kaydetme hatası: {ex.Message}", "error");
            }
        }

        private void RootGrid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Çoklu sıralama: Shift ile mevcut sıralamayı koru
            if (e.Key == System.Windows.Input.Key.F5)
            {
                RefreshProducts_Click(sender, new RoutedEventArgs());
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.U)
            {
                try { ProductUploadWindowManager.TryOpen(Window.GetWindow(this)); } catch { }
                e.Handled = true;
            }
        }

        private async void RetryProducts_Click(object sender, RoutedEventArgs e)
        {
            ProductsErrorState.Visibility = Visibility.Collapsed;
            await LoadProductsAsync();
        }
    }


}