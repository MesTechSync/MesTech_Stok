using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using MesTechStok.Desktop.Models;
using MesTechStok.Desktop.Services;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using MesTechStok.Core.Diagnostics;
using MesTechStok.Core.Services.Abstract;

namespace MesTechStok.Desktop.Views
{
    public partial class ProductUploadPopup : Window
    {
        public static readonly DependencyProperty CoverIndexProperty =
            DependencyProperty.Register(nameof(CoverIndex), typeof(int), typeof(ProductUploadPopup), new PropertyMetadata(-1));

        public int CoverIndex
        {
            get => (int)GetValue(CoverIndexProperty);
            set => SetValue(CoverIndexProperty, value);
        }
        private readonly IServiceProvider? _serviceProvider;
        private readonly List<string> _imageFiles = new();
        private readonly List<string> _videoFiles = new();
        private int _coverIndex = -1;
        private int? _editingProductId = null;
        private bool _isDirty = false;

        // 🔥 A++++ THREAD SAFETY: Helper method to get fresh service per operation  
        private IProductDataService GetProductService()
        {
            if (_serviceProvider != null)
            {
                // WARNING: Caller must dispose the scope properly!
                var scope = _serviceProvider.CreateScope();
                var ctx = scope.ServiceProvider.GetService<MesTechStok.Core.Data.AppDbContext>();
                return ctx != null ? new SqlBackedProductService(ctx) : new EnhancedProductService();
            }
            return new EnhancedProductService();
        }

        public ProductUploadPopup()
        {
            InitializeComponent();
            _serviceProvider = MesTechStok.Desktop.App.ServiceProvider;

            // Basit sözlük listeleri (senkron, donmaya yol açmaz)
            try
            {
                CmbOrigin.ItemsSource = new[] { "TR", "CN", "EU", "US" };
                CmbColor.ItemsSource = new[] { "Renkli", "Siyah", "Beyaz", "Kırmızı", "Mavi" };
                CmbMaterial.ItemsSource = new[] { "Porselen", "Cam", "Plastik", "Metal" };
                CmbVolume.ItemsSource = new[] { "200 ml", "300 ml", "500 ml", "1 L" };
            }
            catch { }

            // Ağ/DB çağrılarını UI yüklenince asenkron çek
            Loaded += ProductUploadPopup_Loaded;
            try { this.Activate(); this.Focus(); } catch { }
        }

        // Düzenleme/ön-dolum için aşırı yüklenmiş ctor
        public ProductUploadPopup(ProductItem existing) : this()
        {
            try
            {
                _editingProductId = existing.Id;
                this.Title = "Ürün Düzenle";
                TxtName.Text = existing.Name;
                TxtBarcode.Text = existing.Barcode;
                TxtSku.Text = existing.Sku;
                // Kategori listesi Loaded’da dolduğu için seçimi sonra yapacağız
                this.Loaded += (_, __) =>
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(existing.Category))
                            CmbCategory.SelectedItem = existing.Category;
                    }
                    catch { }
                };

                TxtSale.Text = existing.SalePrice.ToString();
                TxtPurchase.Text = existing.PurchasePrice.ToString();
                TxtDiscount.Text = existing.DiscountRate.ToString();
                TxtStock.Text = existing.Stock.ToString();
                try { TxtMinStock.Text = existing.MinimumStock.ToString(); } catch { }
                try { TxtBrand.Text = existing.Supplier ?? string.Empty; } catch { }
                TxtDescription.Text = existing.Description ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(existing.ImageUrl))
                {
                    _imageFiles.Add(existing.ImageUrl);
                    _coverIndex = 0; CoverIndex = 0;
                }
                if (!string.IsNullOrWhiteSpace(existing.AdditionalImageUrls))
                {
                    var parts = existing.AdditionalImageUrls.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
                    _imageFiles.AddRange(parts);
                }
                ImageList.ItemsSource = ToBitmapList(_imageFiles);
                try { NoImagesHint.Visibility = ImageList.HasItems ? Visibility.Collapsed : Visibility.Visible; } catch { }
                try { ImageCountText.Text = $" ({_imageFiles.Count})"; } catch { }

                if (!string.IsNullOrWhiteSpace(existing.Origin)) CmbOrigin.SelectedItem = existing.Origin;
                if (!string.IsNullOrWhiteSpace(existing.Material)) CmbMaterial.SelectedItem = existing.Material;
                if (!string.IsNullOrWhiteSpace(existing.VolumeText)) CmbVolume.SelectedItem = existing.VolumeText;
                if (!string.IsNullOrWhiteSpace(existing.Color)) CmbColor.SelectedItem = existing.Color;
                if (!string.IsNullOrWhiteSpace(existing.Sizes))
                {
                    var set = new HashSet<string>(existing.Sizes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                    Chk2S.IsChecked = set.Contains("2S");
                    ChkS.IsChecked = set.Contains("S");
                    ChkM.IsChecked = set.Contains("M");
                    ChkL.IsChecked = set.Contains("L");
                    ChkXL.IsChecked = set.Contains("XL");
                    Chk2XL.IsChecked = set.Contains("2XL");
                    Chk3XL.IsChecked = set.Contains("3XL");
                }
                if (existing.LengthCm.HasValue) TxtLength.Text = existing.LengthCm.Value.ToString();
                if (existing.WidthCm.HasValue) TxtWidth.Text = existing.WidthCm.Value.ToString();
                if (existing.HeightCm.HasValue) TxtHeight.Text = existing.HeightCm.Value.ToString();
                if (existing.Desi.HasValue) TxtDesi.Text = existing.Desi.Value.ToString();
                if (existing.LeadTimeDays.HasValue) TxtLeadTime.Text = existing.LeadTimeDays.Value.ToString();
                TxtShipAddress.Text = existing.ShipAddress ?? string.Empty;
                TxtReturnAddress.Text = existing.ReturnAddress ?? string.Empty;

                // KDV combobox varsayımı
                CmbVat.SelectedIndex = 3; // %20
                UpdateCommissionText();
            }
            catch { }
        }

        // Barkod ön-dolum için aşırı yüklenmiş ctor
        public ProductUploadPopup(string prefillBarcode) : this()
        {
            try
            {
                this.Title = "Yeni Ürün";
                TxtBarcode.Text = prefillBarcode ?? string.Empty;
                this.Loaded += (_, __) =>
                {
                    try { TxtName.Focus(); } catch { }
                };
                CmbVat.SelectedIndex = 3; // %20 varsayım
                UpdateCommissionText();
            }
            catch { }
        }

        private async void ProductUploadPopup_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Busy("Yükleniyor…");

                // 🔥 A++++ THREAD SAFETY: Create fresh service per operation
                var cats = await GetProductService().GetCategoriesAsync();
                CmbCategory.ItemsSource = cats;

                // Düzenleme modunda: güncel görselleri DB'den yeniden yükle
                if (_editingProductId.HasValue)
                {
                    await ReloadExistingImagesAsync(_editingProductId.Value);
                }
            }
            catch { }
            finally { Busy(); }
        }

        private async Task ReloadExistingImagesAsync(int productId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] ReloadExistingImagesAsync START - ProductId: {productId}");

                var dbItem = await GetProductService().GetProductByIdAsync(productId);
                if (dbItem == null)
                {
                    Console.WriteLine("[DEBUG] ReloadExistingImagesAsync - Product not found in DB");
                    return;
                }

                Console.WriteLine($"[DEBUG] Product found - ImageUrl: '{dbItem.ImageUrl}', AdditionalImageUrls: '{dbItem.AdditionalImageUrls}'");

                _imageFiles.Clear();

                // Ana görsel ekle
                if (!string.IsNullOrWhiteSpace(dbItem.ImageUrl))
                {
                    var cleanImageUrl = dbItem.ImageUrl.Trim();
                    Console.WriteLine($"[DEBUG] Adding main image: {cleanImageUrl}");
                    _imageFiles.Add(cleanImageUrl);
                }

                // Ek görseller ekle
                if (!string.IsNullOrWhiteSpace(dbItem.AdditionalImageUrls))
                {
                    var parts = dbItem.AdditionalImageUrls.Split(new[] { ';', ',', '|', '\\', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToArray();

                    Console.WriteLine($"[DEBUG] Adding {parts.Length} additional images: [{string.Join(", ", parts)}]");
                    _imageFiles.AddRange(parts);
                }

                Console.WriteLine($"[DEBUG] Total images loaded: {_imageFiles.Count}");

                // UI'ı güncelle
                ImageList.ItemsSource = null;
                var bitmapList = ToBitmapList(_imageFiles).ToList();
                Console.WriteLine($"[DEBUG] Successfully created {bitmapList.Count} bitmap objects");

                ImageList.ItemsSource = bitmapList;
                _coverIndex = _imageFiles.Count > 0 ? 0 : -1;
                CoverIndex = _coverIndex;

                // UI elementlerini güncelle
                try
                {
                    NoImagesHint.Visibility = ImageList.HasItems ? Visibility.Collapsed : Visibility.Visible;
                    Console.WriteLine($"[DEBUG] NoImagesHint visibility set to: {NoImagesHint.Visibility}");
                }
                catch { }

                try
                {
                    ImageCountText.Text = $" ({_imageFiles.Count})";
                    Console.WriteLine($"[DEBUG] ImageCountText updated to: {ImageCountText.Text}");
                }
                catch { }

                Console.WriteLine("[DEBUG] ReloadExistingImagesAsync COMPLETED successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ReloadExistingImagesAsync failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                MesTechStok.Desktop.Utils.ToastManager.ShowError($"Ürün resimleri yüklenirken hata: {ex.Message}", "Resim Yükleme");
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                e.Handled = true; SaveAndClose_Click(this, new RoutedEventArgs());
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                e.Handled = true; SaveAndClose_Click(this, new RoutedEventArgs());
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D)
            {
                e.Handled = true; SaveDraft_Click(this, new RoutedEventArgs());
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isDirty)
                {
                    var r = MessageBox.Show("Kaydedilmemiş değişiklikler var. Kapatmak istiyor musunuz?", "Uyarı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (r != MessageBoxResult.Yes) return;
                }
            }
            catch { }
            Close();
        }

        private void Busy(string? text = null)
        {
            BusyOverlay.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
            BusyText.Text = text ?? string.Empty;
        }

        private void AddImages_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = true
            };
            if (ofd.ShowDialog() == true)
            {
                _imageFiles.AddRange(ofd.FileNames);
                ImageList.ItemsSource = null;
                ImageList.ItemsSource = _imageFiles.Select(f => new System.Windows.Media.Imaging.BitmapImage(new Uri(f)));
                try { NoImagesHint.Visibility = ImageList.HasItems ? Visibility.Collapsed : Visibility.Visible; } catch { }
                try { ImageCountText.Text = $" ({_imageFiles.Count})"; } catch { }
                _isDirty = true;
                AnyField_Changed(this, new RoutedEventArgs());
            }
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (_imageFiles.Count == 0) return;
            try
            {
                var images = ToBitmapList(_imageFiles).ToList();
                if (images.Count == 0) return;
                var viewer = new ProductImageViewer(images);
                viewer.Owner = this;
                viewer.Show();
            }
            catch { }
        }

        private void Image_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Image img && img.Source is BitmapImage bi)
                {
                    var list = new List<BitmapImage> { bi };
                    var viewer = new ProductImageViewer(list);
                    viewer.Owner = this;
                    viewer.Show();
                }
            }
            catch { }
        }

        private async void SaveDraft_Click(object sender, RoutedEventArgs e)
        {
            await SaveInternalAsync(draft: true);
        }

        private async void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            if (await SaveInternalAsync(draft: false)) Close();
        }

        private async Task<bool> SaveInternalAsync(bool draft)
        {
            try
            {
                using var corr = CorrelationContext.StartNew($"PROD-{Guid.NewGuid():N}".Substring(0, 12));
                var userName = await GetCurrentUsernameAsync();

                // DEBUG LOG: Save işlemi başlangıcı
                Console.WriteLine($"[DEBUG] ProductUploadPopup Save Start - EditingId: {_editingProductId}, ImageCount: {_imageFiles.Count}");

                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("PRODUCT_SAVE_START",
                    $"corrId={CorrelationContext.CurrentId} user={userName} mode={(_editingProductId.HasValue ? "edit" : (draft ? "draft" : "add"))}",
                    nameof(ProductUploadPopup));
                // Esnek doğrulama: Barkod zorunlu, Ad boşsa barkodu ad olarak kullan
                var name = TxtName.Text?.Trim() ?? string.Empty;
                var barcode = TxtBarcode.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    MesTechStok.Desktop.Utils.ToastManager.ShowWarning("Barkod gerekli", "Ürün");
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("PRODUCT_VALIDATION_FAIL",
                        $"corrId={CorrelationContext.CurrentId} user={userName} reason=MissingBarcode", nameof(ProductUploadPopup));
                    try { TxtBarcode.Focus(); } catch { }
                    return false;
                }
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = barcode;
                    try { TxtName.Text = name; } catch { }
                }
                decimal.TryParse(TxtSale.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var sale);
                decimal.TryParse(TxtPurchase.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var purchase);
                decimal.TryParse(TxtDiscount.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var discount);
                int.TryParse(TxtStock.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.CurrentCulture, out var stock);

                // Görsel seçimleri: kapak + ekler (dosya yolları)
                string? coverPath = null;
                string? additionalPaths = null;
                // Görsel kaynakları: tek liste
                var allImages = _imageFiles.ToList();

                // DEBUG LOG: Image handling
                Console.WriteLine($"[DEBUG] Image processing - AllImages.Count: {allImages.Count}, CoverIndex: {_coverIndex}");
                if (allImages.Count > 0)
                {
                    foreach (var img in allImages.Select((path, idx) => new { Path = path, Index = idx }))
                    {
                        Console.WriteLine($"[DEBUG] Image {img.Index}: {img.Path}");
                    }
                }

                if (allImages.Count > 0)
                {
                    coverPath = (_coverIndex >= 0 && _coverIndex < allImages.Count) ? allImages[_coverIndex] : allImages[0];
                    var others = allImages.Where((p, idx) => idx != (_coverIndex >= 0 ? _coverIndex : 0)).ToList();
                    if (others.Count > 0) additionalPaths = string.Join(",", others);
                }

                // DEBUG LOG: Final paths
                Console.WriteLine($"[DEBUG] CoverPath: {coverPath}");
                Console.WriteLine($"[DEBUG] AdditionalPaths: {additionalPaths}");

                var item = new ProductItem
                {
                    Name = name,
                    Barcode = barcode,
                    Sku = TxtSku.Text?.Trim() ?? string.Empty,
                    Category = CmbCategory.SelectedItem?.ToString() ?? string.Empty,
                    Price = Math.Max(0, sale),
                    PurchasePrice = Math.Max(0, purchase),
                    DiscountRate = Math.Clamp(discount, 0, 100),
                    Stock = Math.Max(0, stock),
                    MinimumStock = TryParseNullableInt(TxtMinStock.Text) ?? 10,
                    Supplier = TxtBrand.Text?.Trim() ?? string.Empty,
                    Description = TxtDescription.Text,
                    ImageUrl = coverPath,
                    AdditionalImageUrls = additionalPaths,
                    Origin = CmbOrigin.SelectedItem?.ToString(),
                    Material = CmbMaterial.SelectedItem?.ToString(),
                    VolumeText = CmbVolume.SelectedItem?.ToString(),
                    Color = CmbColor.SelectedItem?.ToString(),
                    Sizes = CollectSizes(),
                    LengthCm = TryParseNullableDecimal(TxtLength.Text),
                    WidthCm = TryParseNullableDecimal(TxtWidth.Text),
                    HeightCm = TryParseNullableDecimal(TxtHeight.Text),
                    Desi = TryParseNullableDecimal(TxtDesi.Text),
                    LeadTimeDays = TryParseNullableInt(TxtLeadTime.Text),
                    ShipAddress = TxtShipAddress.Text,
                    ReturnAddress = TxtReturnAddress.Text
                };
                // Link-only ekleme modunda kayıttan önce opsiyonel yerel kayıt uyarılarını kullanıcıya bırakıyoruz
                // Denetim bilgileri (opsiyonel)
                try { item.UsageInstructions = TxtUsage.Text?.Trim(); } catch { }
                try { item.ImporterInfo = TxtImporter.Text?.Trim(); } catch { }
                try { item.ManufacturerInfo = TxtManufacturer.Text?.Trim(); } catch { }

                Busy("Kaydediliyor…");
                bool ok;
                if (_editingProductId.HasValue)
                {
                    item.Id = _editingProductId.Value;
                    ok = await GetProductService().UpdateProductAsync(item);
                    // Demo ürünü gibi kimliği olmayan veya bulunamayan kayıtlar için barkoda göre güvenli upsert
                    if (!ok)
                    {
                        var existingByBarcode = await GetProductService().GetProductByBarcodeAsync(barcode);
                        if (existingByBarcode != null)
                        {
                            item.Id = existingByBarcode.Id;
                            ok = await GetProductService().UpdateProductAsync(item);
                        }
                        else
                        {
                            ok = await GetProductService().AddProductAsync(item);
                        }
                    }
                }
                else
                {
                    var existing = await GetProductService().GetProductByBarcodeAsync(barcode);
                    if (existing != null)
                    {
                        item.Id = existing.Id;
                        ok = await GetProductService().UpdateProductAsync(item);
                    }
                    else
                    {
                        ok = await GetProductService().AddProductAsync(item);
                    }
                }
                if (!ok)
                {
                    // Servis başarısızsa detaylı mesaj göster (kısıt/çakışma vs.)
                    MesTechStok.Desktop.Utils.ToastManager.ShowWarning("Kayıt yapılamadı (muhtemel veri kısıtı). Zorunlu tek alan: Barkod.", "Ürün");
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("PRODUCT_SAVE_FAIL",
                        $"corrId={CorrelationContext.CurrentId} user={userName} barcode={barcode}", nameof(ProductUploadPopup));
                    return false;
                }
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("PRODUCT_DB_OK",
                    $"corrId={CorrelationContext.CurrentId} user={userName} productId={item.Id}", nameof(ProductUploadPopup));

                // Görselleri (varsa) kalıcı olarak kaydet ve ürün alanlarına gerçek yolları yaz
                if (allImages.Count > 0 && !string.IsNullOrWhiteSpace(coverPath))
                {
                    try
                    {
                        var storage = new ImageStorageService();
                        // Kapak
                        var coverSaved = await storage.SaveAsync(item.Id, coverPath);
                        var mediaChanged = false;
                        if (!string.IsNullOrWhiteSpace(coverSaved.Full1200))
                        {
                            item.ImageUrl = coverSaved.Full1200;
                            mediaChanged = true;
                        }
                        else
                        {
                            try { MesTechStok.Desktop.Utils.ToastManager.ShowWarning("Kapak görseli kaydedilemedi (boyut/uzantı limiti).", "Görsel"); } catch { }
                        }

                        // Ek görseller
                        var others = allImages.Where((p, idx) => idx != (_coverIndex >= 0 ? _coverIndex : 0)).ToList();
                        if (others.Count > 0)
                        {
                            var savedPaths = new List<string>();
                            var failedCount = 0;
                            foreach (var f in others)
                            {
                                try
                                {
                                    var r = await storage.SaveAsync(item.Id, f);
                                    if (!string.IsNullOrWhiteSpace(r.Full1200)) savedPaths.Add(r.Full1200!);
                                    else failedCount++;
                                }
                                catch { }
                            }
                            if (savedPaths.Count > 0)
                            {
                                item.AdditionalImageUrls = string.Join(";", savedPaths);
                                mediaChanged = true;
                            }
                            if (failedCount > 0)
                            {
                                try { MesTechStok.Desktop.Utils.ToastManager.ShowWarning($"{failedCount} görsel kaydedilemedi (boyut/uzantı limiti).", "Görsel"); } catch { }
                            }
                        }

                        // DB'de görsel yollarını güncelle
                        if (mediaChanged)
                        {
                            try { await GetProductService().UpdateProductAsync(item); } catch { }
                        }
                    }
                    catch { }
                }

                var op = _editingProductId.HasValue ? "Update" : (draft ? "Draft" : "Add");
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo(
                    $"[PRODUCT] {op} Name={item.Name} Barcode={item.Barcode}",
                    nameof(ProductUploadPopup));
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("PRODUCT_SAVE_END",
                    $"corrId={CorrelationContext.CurrentId} user={userName} productId={item.Id}", nameof(ProductUploadPopup));

                MesTechStok.Desktop.Utils.ToastManager.ShowSuccess("💾 Kayıt başarılı", "Ürün");
                MesTechStok.Desktop.Utils.EventBus.PublishProductsChanged(item.Barcode);
                _isDirty = false;
                return true;
            }
            catch (Exception ex)
            {
                MesTechStok.Desktop.Utils.ToastManager.ShowError($"Kaydetme hatası: {ex.Message}", "Ürün");
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("PRODUCT_SAVE_EXCEPTION", $"corrId={CorrelationContext.CurrentId} msg={ex.Message}", nameof(ProductUploadPopup)); } catch { }
                return false;
            }
            finally
            {
                Busy();
            }
        }

        private async Task<string> GetCurrentUsernameAsync()
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var auth = sp?.GetService<IAuthService>();
                if (auth == null) return "anonymous";
                var user = await auth.GetCurrentUserAsync();
                return user?.Username ?? "anonymous";
            }
            catch { return "anonymous"; }
        }

        private static decimal? TryParseNullableDecimal(string? s)
        {
            if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var v)) return v;
            return null;
        }
        private static int? TryParseNullableInt(string? s)
        {
            if (int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.CurrentCulture, out var v)) return v;
            return null;
        }

        private string? CollectSizes()
        {
            try
            {
                var list = new List<string>();
                void add(CheckBox cb) { if (cb.IsChecked == true && cb.Content is string s) list.Add(s); }
                add(Chk2S); add(ChkS); add(ChkM); add(ChkL); add(ChkXL); add(Chk2XL); add(Chk3XL);
                return list.Count > 0 ? string.Join(",", list) : null;
            }
            catch { return null; }
        }

        // Basit komisyon/net/marj hesap gösterimi
        private void UpdateCommissionText()
        {
            decimal.TryParse(TxtSale.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var sale);
            decimal.TryParse(TxtPurchase.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var purchase);
            decimal.TryParse(TxtDiscount.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var discount);

            var vatPct = ParsePercent(CmbVat.SelectedItem as ComboBoxItem);
            var discounted = sale * (1 - Math.Clamp(discount, 0, 100) / 100m);
            var vat = discounted * vatPct / 100m;
            var commissionPct = 21m; // ileride dinamik
            var commission = discounted * commissionPct / 100m;
            var net = Math.Max(0, discounted - vat - commission);
            var margin = discounted > 0 ? Math.Max(0, discounted - purchase) : 0m;
            var marginPct = discounted > 0 ? (margin / discounted) * 100m : 0m;
            CommissionText.Text = $"KDV: ₺{vat:N2} · Komisyon: ₺{commission:N2}";
            try { NetSummaryText.Text = $"Net: ₺{net:N2} · Marj: ₺{margin:N2} (%{marginPct:0.##})"; } catch { }
        }

        private static decimal ParsePercent(ComboBoxItem? item)
        {
            if (item?.Content is string s && s.Trim().StartsWith("%"))
            {
                if (decimal.TryParse(s.Trim('%'), out var val)) return val;
            }
            return 0m;
        }

        private void CmbVat_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateCommissionText();
        private void TxtSale_TextChanged(object sender, TextChangedEventArgs e) => UpdateCommissionText();
        private void TxtDiscount_TextChanged(object sender, TextChangedEventArgs e) => UpdateCommissionText();

        // Numerik giriş doğrulamaları
        private static readonly Regex IntegerRegex = new Regex("^[0-9]+$");
        private void Decimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var sep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var pattern = new Regex("^[0-9]+(" + Regex.Escape(sep) + "?[0-9]*)?$");
            var current = (sender as TextBox)?.Text ?? string.Empty;
            e.Handled = !pattern.IsMatch(current + e.Text);
        }
        private void Integer_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var current = (sender as TextBox)?.Text ?? string.Empty;
            e.Handled = !IntegerRegex.IsMatch(current + e.Text);
        }
        private void Decimal_Paste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string))!;
                var sep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                var pattern = new Regex("^[0-9]+(" + Regex.Escape(sep) + "?[0-9]*)?$");
                if (!pattern.IsMatch(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }
        private void Integer_Paste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string))!;
                if (!IntegerRegex.IsMatch(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private void Sizes_All_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                bool on = (sender as CheckBox)?.IsChecked == true;
                Chk2S.IsChecked = on;
                ChkS.IsChecked = on;
                ChkM.IsChecked = on;
                ChkL.IsChecked = on;
                ChkXL.IsChecked = on;
                Chk2XL.IsChecked = on;
                Chk3XL.IsChecked = on;
            }
            catch { }
        }

        private void Root_DragOver(object sender, DragEventArgs e) { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy; }
        private void Root_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var imgs = files.Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                    _imageFiles.AddRange(imgs);
                    ImageList.ItemsSource = null;
                    ImageList.ItemsSource = _imageFiles.Select(f => new BitmapImage(new Uri(f)));
                    _isDirty = true;
                    AnyField_Changed(this, new RoutedEventArgs());
                }
            }
            catch { }
        }

        // Basit drag-sort desteği
        private Point _dragStart;
        private void ImageItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
        }
        private void ImageItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(null);
            if (Math.Abs(pos.X - _dragStart.X) > 4 || Math.Abs(pos.Y - _dragStart.Y) > 4)
            {
                if (sender is FrameworkElement fe && fe.DataContext is BitmapImage bi)
                {
                    try { Mouse.SetCursor(Cursors.SizeAll); } catch { }
                    // Eğer kaynak öğe seçili değilse sadece onu seç
                    if (!ImageList.SelectedItems.Contains(bi))
                    {
                        ImageList.SelectedItems.Clear();
                        ImageList.SelectedItem = bi;
                    }
                    var selectedIndices = ImageList.SelectedItems
                        .Cast<object>()
                        .Select(x => ImageList.Items.IndexOf(x))
                        .Where(i => i >= 0)
                        .OrderBy(i => i)
                        .ToArray();
                    var data = new DataObject();
                    data.SetData("MES_IMG_IDX", selectedIndices);
                    // uyumluluk için ilk öğeyi de taşı
                    data.SetData(typeof(BitmapImage), bi);
                    DragDrop.DoDragDrop(fe, data, DragDropEffects.Move);
                }
            }
        }
        private void ImageItem_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("MES_IMG_IDX") || e.Data.GetDataPresent(typeof(BitmapImage)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else e.Effects = DragDropEffects.None;
        }
        private void ImageItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is BitmapImage target)
            {
                var destIdx = ImageList.Items.IndexOf(target);
                if (destIdx < 0) return;
                // Çoklu sürükle-bırak
                if (e.Data.GetDataPresent("MES_IMG_IDX"))
                {
                    var payload = e.Data.GetData("MES_IMG_IDX");
                    int[] moved = Array.Empty<int>();
                    if (payload is int[] arr) moved = arr;
                    else if (payload is System.Collections.IEnumerable en)
                    {
                        var list = new List<int>();
                        foreach (var it in en) if (it is int v) list.Add(v);
                        moved = list.OrderBy(x => x).ToArray();
                    }
                    if (moved.Length == 0) return;
                    // Seçim hedefin üzerine ise işlem yapma
                    if (moved.Contains(destIdx)) return;
                    var coverPathBefore = (_coverIndex >= 0 && _coverIndex < _imageFiles.Count) ? _imageFiles[_coverIndex] : null;
                    // Taşınacak blok
                    var block = moved.Select(i => _imageFiles[i]).ToList();
                    // Önce yüksekten düşüğe sil
                    for (int i = moved.Length - 1; i >= 0; i--)
                    {
                        _imageFiles.RemoveAt(moved[i]);
                    }
                    // Hedef, silinen öğeler kaynakta hedefin solundaysa kayar
                    int shift = moved.Count(i => i < destIdx);
                    int insertAt = destIdx - shift;
                    if (insertAt < 0) insertAt = 0;
                    if (insertAt > _imageFiles.Count) insertAt = _imageFiles.Count;
                    // Bloku sırayla ekle
                    for (int i = 0; i < block.Count; i++)
                    {
                        _imageFiles.Insert(insertAt + i, block[i]);
                    }
                    // Seçimi geri yükle
                    Dispatcher.InvokeAsync(() =>
                    {
                        ImageList.ItemsSource = null;
                        ImageList.ItemsSource = _imageFiles.Select(f => new BitmapImage(new Uri(f)));
                        ImageList.SelectedItems.Clear();
                        for (int i = 0; i < block.Count; i++)
                        {
                            var idx = insertAt + i;
                            if (idx >= 0 && idx < ImageList.Items.Count)
                                ImageList.SelectedItems.Add(ImageList.Items[idx]);
                        }
                        // Kapak indexini güncelle (kapak aynı dosyaysa yeni konumunu bul)
                        if (!string.IsNullOrWhiteSpace(coverPathBefore))
                        {
                            var newIdx = _imageFiles.FindIndex(p => string.Equals(p, coverPathBefore, StringComparison.OrdinalIgnoreCase));
                            if (newIdx >= 0) { _coverIndex = newIdx; CoverIndex = newIdx; }
                        }
                        _isDirty = true;
                    }, System.Windows.Threading.DispatcherPriority.Background);
                    try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("IMAGE", $"DragMoveMulti count={moved.Length} dest={destIdx}", nameof(ProductUploadPopup)); AppendAudit($"Çoklu sürükle-bırak: {moved.Length} öğe"); } catch { }
                    return;
                }
                // Tekli sürükle-bırak (mevcut davranış)
                if (e.Data.GetDataPresent(typeof(BitmapImage)))
                {
                    var source = (BitmapImage)e.Data.GetData(typeof(BitmapImage))!;
                    var srcIdx = ImageList.Items.IndexOf(source);
                    if (srcIdx >= 0 && srcIdx != destIdx)
                    {
                        var coverPathBefore = (_coverIndex >= 0 && _coverIndex < _imageFiles.Count) ? _imageFiles[_coverIndex] : null;
                        var tmp = _imageFiles[srcIdx];
                        _imageFiles.RemoveAt(srcIdx);
                        // yeniden hedef konuma ekle (src < dest ise kayma var)
                        int insertAt = destIdx;
                        if (srcIdx < destIdx) insertAt = destIdx - 1;
                        _imageFiles.Insert(insertAt, tmp);
                        Dispatcher.InvokeAsync(() =>
                        {
                            ImageList.ItemsSource = null;
                            ImageList.ItemsSource = _imageFiles.Select(f => new BitmapImage(new Uri(f)));
                            // Kapak konumunu güncelle
                            if (!string.IsNullOrWhiteSpace(coverPathBefore))
                            {
                                var newIdx = _imageFiles.FindIndex(p => string.Equals(p, coverPathBefore, StringComparison.OrdinalIgnoreCase));
                                if (newIdx >= 0) { _coverIndex = newIdx; CoverIndex = newIdx; }
                            }
                            _isDirty = true;
                        }, System.Windows.Threading.DispatcherPriority.Background);
                        try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("IMAGE", $"DragMoveSingle from={srcIdx} to={insertAt}", nameof(ProductUploadPopup)); AppendAudit($"Sürükle-bırak: {srcIdx}→{insertAt}"); } catch { }
                    }
                }
            }
        }

        // Tek tıkla kapak seçimi
        private void ImageItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement fe && fe.DataContext is BitmapImage bi)
                {
                    var idx = ImageList.Items.IndexOf(bi);
                    if (idx >= 0) { _coverIndex = idx; CoverIndex = idx; _isDirty = true; }
                }
            }
            catch { }
        }

        private void RemoveAllImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _imageFiles.Clear();
                ImageList.ItemsSource = null;
                _coverIndex = -1; CoverIndex = -1;
                _isDirty = true;
            }
            catch { }
        }

        // Yeni: Video ekle (şimdilik dosya seçtirip loglama yapıyoruz)
        private void AddVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "Video|*.mp4;*.mov;*.avi;*.mkv",
                    Multiselect = false
                };
                if (ofd.ShowDialog() == true)
                {
                    ValidateAndAddVideoAsync(ofd.FileName);
                    try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("VIDEO", $"Add path={System.IO.Path.GetFileName(ofd.FileName)}", nameof(ProductUploadPopup)); AppendAudit($"Video eklendi: {System.IO.Path.GetFileName(ofd.FileName)}"); } catch { }
                }
            }
            catch { }
        }

        // Link ile resim ekleme (indirmeden göster veya indirerek kaydet seçenekli)
        private void AddImageFromLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = Microsoft.VisualBasic.Interaction.InputBox("Görsel URL'si girin:", "Linkten Görsel Ekle", "https://");
                if (string.IsNullOrWhiteSpace(url)) return;
                var choice = System.Windows.MessageBox.Show("Görseli bilgisayara kaydetmek ister misiniz?\nEvet: İndirip yerel kopyasını kullan.\nHayır: Sadece link üzerinden göster.", "Görsel Ekleme", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (choice == MessageBoxResult.Cancel) return;
                // Her iki durumda da UI listesine ekle; kaydettiğinizde indirilecektir
                _imageFiles.Add(url);
                ImageList.ItemsSource = null;
                ImageList.ItemsSource = _imageFiles.Select(p => new BitmapImage(new Uri(p))).ToList();
                _isDirty = true;
                MesTechStok.Desktop.Utils.ToastManager.ShowSuccess("Görsel linki eklendi", "Görsel");
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("IMAGE", $"AddLink url={url}", nameof(ProductUploadPopup)); AppendAudit($"Linkten görsel eklendi"); } catch { }
            }
            catch { }
        }

        private void AddVideoFromLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = Microsoft.VisualBasic.Interaction.InputBox("Video URL'si girin:", "Linkten Video Ekle", "https://");
                if (string.IsNullOrWhiteSpace(url)) return;
                var choice = System.Windows.MessageBox.Show("Videoyu bilgisayara kaydetmek ister misiniz?\nEvet: İndirip yerel kopyasını kullan.\nHayır: Sadece link üzerinden göster.", "Video Ekleme", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (choice == MessageBoxResult.Cancel) return;
                // Her iki durumda da UI listesine ekle; oynatma linkten olur
                _videoFiles.Add(url);
                VideoList.ItemsSource = null;
                VideoList.ItemsSource = _videoFiles.ToList();
                _isDirty = true;
                MesTechStok.Desktop.Utils.ToastManager.ShowSuccess("Video linki eklendi", "Video");
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("VIDEO", $"AddLink url={url}", nameof(ProductUploadPopup)); AppendAudit($"Linkten video eklendi"); } catch { }
            }
            catch { }
        }

        private async void ValidateAndAddVideoAsync(string filePath)
        {
            try
            {
                Busy("Video doğrulanıyor…");
                var ok = await ValidateVideoDurationAsync(filePath, maxSeconds: 30);
                if (!ok)
                {
                    MesTechStok.Desktop.Utils.ToastManager.ShowWarning("Video uzunluğu 30 saniyeyi aşamaz", "Video");
                    return;
                }
                _videoFiles.Add(filePath);
                VideoList.ItemsSource = null;
                VideoList.ItemsSource = _videoFiles;
                MesTechStok.Desktop.Utils.ToastManager.ShowSuccess("Video eklendi", "Video");
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("VIDEO", $"Validated path={System.IO.Path.GetFileName(filePath)}", nameof(ProductUploadPopup)); } catch { }
            }
            catch { }
            finally { Busy(); }
        }

        private Task<bool> ValidateVideoDurationAsync(string filePath, int maxSeconds)
        {
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                var player = new System.Windows.Media.MediaPlayer();
                player.MediaOpened += (_, __) =>
                {
                    try
                    {
                        var dur = player.NaturalDuration;
                        if (dur.HasTimeSpan)
                        {
                            tcs.TrySetResult(dur.TimeSpan.TotalSeconds <= maxSeconds);
                        }
                        else tcs.TrySetResult(false);
                    }
                    catch { tcs.TrySetResult(false); }
                    finally { try { player.Close(); } catch { } }
                };
                player.MediaFailed += (_, __) => { try { player.Close(); } catch { } tcs.TrySetResult(false); };
                player.Open(new Uri(filePath));
            }
            catch { tcs.TrySetResult(false); }
            return tcs.Task;
        }

        private void PlayVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fe = sender as FrameworkElement;
                var path = fe?.DataContext as string;
                if (string.IsNullOrWhiteSpace(path)) return;
                // Dış oynatıcı ile aç (varsayılan Windows Player)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("VIDEO", $"Play path={System.IO.Path.GetFileName(path)}", nameof(ProductUploadPopup)); AppendAudit($"Video oynatıldı: {System.IO.Path.GetFileName(path)}"); } catch { }
            }
            catch { }
        }

        private void RemoveVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fe = sender as FrameworkElement;
                var path = fe?.DataContext as string;
                if (string.IsNullOrWhiteSpace(path)) return;
                _videoFiles.Remove(path);
                VideoList.ItemsSource = null;
                VideoList.ItemsSource = _videoFiles;
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("VIDEO", $"Removed path={System.IO.Path.GetFileName(path)}", nameof(ProductUploadPopup)); AppendAudit($"Video silindi: {System.IO.Path.GetFileName(path)}"); } catch { }
            }
            catch { }
        }

        // Sol menü hızlı gezinme (basit odak kaydırma)
        private void ScrollToElement(FrameworkElement el)
        {
            try
            {
                el?.BringIntoView();
            }
            catch { }
        }
        private void Nav_UrunBilgileri_Click(object sender, RoutedEventArgs e) => ScrollToElement(Sec_UrunBilgileri);
        private void Nav_SatisBilgileri_Click(object sender, RoutedEventArgs e) => ScrollToElement(Sec_SatisBilgileri);
        private void Nav_UrunOzellikleri_Click(object sender, RoutedEventArgs e) => ScrollToElement(Sec_UrunOzellikleri);
        private void Nav_Kargo_Click(object sender, RoutedEventArgs e) => ScrollToElement(Sec_Kargo);
        private void Nav_Aciklama_Click(object sender, RoutedEventArgs e) => ScrollToElement(Sec_Aciklama);
        private void Nav_Denetim_Click(object sender, RoutedEventArgs e) => ScrollToElement(Sec_Denetim);
        private void Nav_Gecmis_Click(object sender, RoutedEventArgs e) => ScrollToElement(Sec_Gecmis);

        // Doluluk oranı hesaplama (Trendyol benzeri basit metrik)
        private void AnyField_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                int images = _imageFiles.Count;
                int attrs = 0;
                if (!string.IsNullOrWhiteSpace(CmbOrigin?.SelectedItem?.ToString())) attrs++;
                if (!string.IsNullOrWhiteSpace(CmbMaterial?.SelectedItem?.ToString())) attrs++;
                if (!string.IsNullOrWhiteSpace(CmbVolume?.SelectedItem?.ToString())) attrs++;
                if (!string.IsNullOrWhiteSpace(CmbColor?.SelectedItem?.ToString())) attrs++;
                // WHL tam girildiyse (üçü de geçerli sayı) +1
                bool hasL = TryParseNullableDecimal(TxtLength.Text).HasValue;
                bool hasW = TryParseNullableDecimal(TxtWidth.Text).HasValue;
                bool hasH = TryParseNullableDecimal(TxtHeight.Text).HasValue;
                if (hasL && hasW && hasH) attrs++;
                int desc = string.IsNullOrWhiteSpace(TxtDescription?.Text) ? 0 : 1;

                // kaba bir yüzde: görsel (40), özellikler (40), açıklama (20)
                int pct = Math.Min(100, (int)Math.Round(Math.Min(1, images / 5.0) * 40 + (attrs / 5.0) * 40 + (desc * 20)));
                FillRateProgress.Value = pct;
                string level = pct < 50 ? "Zayıf" : (pct < 80 ? "Orta" : "İyi");
                FillRateText.Text = $"{level} (%{pct})";
                FillImgBadge.Text = images.ToString();
                FillAttrBadge.Text = $"{attrs}/5";
                FillDescBadge.Text = desc == 1 ? "Var" : "Yok";
                _isDirty = true;

                // Girilen alanlar özetini güncelle
                var entered = new List<string>();
                void add(string title, string? val) { if (!string.IsNullOrWhiteSpace(val)) entered.Add($"{title}: {val}"); }
                add("Ad", TxtName?.Text);
                add("Barkod", TxtBarcode?.Text);
                add("SKU", TxtSku?.Text);
                add("Kategori", CmbCategory?.SelectedItem?.ToString());
                add("Marka", TxtBrand?.Text);
                add("Menşei", CmbOrigin?.SelectedItem?.ToString());
                add("Renk", CmbColor?.SelectedItem?.ToString());
                add("Materyal", CmbMaterial?.SelectedItem?.ToString());
                add("Hacim", CmbVolume?.SelectedItem?.ToString());
                if (TryParseNullableDecimal(TxtDesi?.Text ?? string.Empty).HasValue) entered.Add($"Desi: {TxtDesi?.Text}");
                if (TryParseNullableInt(TxtLeadTime?.Text ?? string.Empty).HasValue) entered.Add($"Termin: {TxtLeadTime?.Text} gün");
                if (EnteredList != null) EnteredList.ItemsSource = entered;
            }
            catch { }
        }

        // Barkod dinleme toggle
        private async void ChkBarcodeListen_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var svc = sp?.GetService<MesTechStok.Desktop.Services.IBarcodeService>();
                if (svc == null) { MesTechStok.Desktop.Utils.ToastManager.ShowWarning("Barkod servisi yok", "Barkod"); return; }
                svc.BarcodeScanned += BarcodeSvc_BarcodeScanned;
                if (!svc.IsConnected) await svc.ConnectAsync();
                await svc.StartScanningAsync();
                MesTechStok.Desktop.Utils.ToastManager.ShowInfo("Barkod dinleme aktif", "Barkod");
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
                svc.BarcodeScanned -= BarcodeSvc_BarcodeScanned;
                await svc.DisconnectAsync();
                MesTechStok.Desktop.Utils.ToastManager.ShowInfo("Barkod dinleme kapalı", "Barkod");
            }
            catch { }
        }

        private async void BarcodeSvc_BarcodeScanned(object? sender, BarcodeScannedEventArgs e)
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    LblLastBarcode.Text = e.Barcode;

                    // Mevcut barkod var mı kontrol et
                    var currentBarcode = (TxtBarcode.Text ?? "").Trim();
                    bool shouldUpdate = true;

                    if (!string.IsNullOrWhiteSpace(currentBarcode) && currentBarcode != e.Barcode)
                    {
                        // Kullanıcıya mevcut barkodu değiştirmek isteyip istemediğini sor
                        var result = MessageBox.Show(
                            $"Mevcut barkod numarası: {currentBarcode}\nOkutulan yeni barkod: {e.Barcode}\n\nMevcut barkod numarasını yenisiyle değiştirmek istiyor musunuz?",
                            "Barkod Değiştirme Onayı",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        shouldUpdate = result == MessageBoxResult.Yes;

                        if (!shouldUpdate)
                        {
                            MesTechStok.Desktop.Utils.ToastManager.ShowInfo("Barkod değiştirilmedi, mevcut barkod korundu.", "Barkod");
                            return;
                        }
                    }

                    if (shouldUpdate)
                    {
                        TxtBarcode.Text = e.Barcode;
                        MesTechStok.Desktop.Utils.ToastManager.ShowSuccess($"Barkod güncellendi: {e.Barcode}", "Barkod");
                    }

                    // Mevcut ürün var mı? getir ve doldur; yoksa barkodu ad olarak öner
                    var existing = await GetProductService().GetProductByBarcodeAsync(e.Barcode);
                    if (existing != null)
                    {
                        TxtName.Text = existing.Name;
                        TxtSku.Text = existing.Sku;
                        if (!string.IsNullOrWhiteSpace(existing.Category)) CmbCategory.SelectedItem = existing.Category;
                        TxtSale.Text = existing.SalePrice.ToString();
                        TxtPurchase.Text = existing.PurchasePrice.ToString();
                        TxtDiscount.Text = existing.DiscountRate.ToString();
                        TxtStock.Text = existing.Stock.ToString();
                        TxtBrand.Text = existing.Supplier ?? string.Empty;
                        TxtDescription.Text = existing.Description ?? string.Empty;
                        // Görseller
                        _imageFiles.Clear();
                        if (!string.IsNullOrWhiteSpace(existing.ImageUrl)) _imageFiles.Add(existing.ImageUrl);
                        if (!string.IsNullOrWhiteSpace(existing.AdditionalImageUrls))
                        {
                            var parts = existing.AdditionalImageUrls.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
                            _imageFiles.AddRange(parts);
                        }
                        ImageList.ItemsSource = null;
                        ImageList.ItemsSource = ToBitmapList(_imageFiles);
                        try { NoImagesHint.Visibility = ImageList.HasItems ? Visibility.Collapsed : Visibility.Visible; } catch { }
                        try { ImageCountText.Text = $" ({_imageFiles.Count})"; } catch { }
                        _coverIndex = _imageFiles.Count > 0 ? 0 : -1; CoverIndex = _coverIndex;
                        MesTechStok.Desktop.Utils.ToastManager.ShowSuccess("Var olan ürün getirildi", "Barkod");
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(TxtName.Text)) TxtName.Text = e.Barcode;
                        MesTechStok.Desktop.Utils.ToastManager.ShowInfo("Yeni ürün: alanlar doldurulabilir (isim barkoddan alındı)", "Barkod");
                    }
                });
            }
            catch { }
        }

        // Alt çubuk aksiyonları (placeholder davranışlar)
        private void Archive_Click(object sender, RoutedEventArgs e)
        {
            MesTechStok.Desktop.Utils.ToastManager.ShowInfo("Ürün arşivlendi (demo)", "Ürün");
        }
        private async void CopyProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var barcode = (TxtBarcode.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(barcode)) { MesTechStok.Desktop.Utils.ToastManager.ShowWarning("Önce ürün barkodu girin", "Ürün"); return; }
                var existing = await GetProductService().GetProductByBarcodeAsync(barcode);
                if (existing == null) { MesTechStok.Desktop.Utils.ToastManager.ShowWarning("Kaydedilmiş ürün bulunamadı", "Ürün"); return; }
                var clone = existing.Clone();
                clone.Id = 0;
                clone.Barcode = barcode + "-COPY";
                await GetProductService().AddProductAsync(clone);
                MesTechStok.Desktop.Utils.ToastManager.ShowSuccess("Ürün kopyalandı", "Ürün");
                MesTechStok.Desktop.Utils.EventBus.PublishProductsChanged(clone.Barcode);
            }
            catch { }
        }
        // Onaya Gönder kaldırıldı

        private void OpenDropshipping_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var barcode = (TxtBarcode.Text ?? string.Empty).Trim();
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("PRODUCT_AUDIT", $"DropshippingDispatched Barcode={barcode} User={Environment.UserName}", nameof(ProductUploadPopup));
                MesTechStok.Desktop.Utils.ToastManager.ShowSuccess("Dropshipping’e gönderildi (satışa açma talebi iletildi)", "Dropshipping");
                AppendAudit($"Dropshipping’e gönderildi: {barcode}");
            }
            catch { }
        }

        private void ShipAddress_Select_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Adres listesi (örnek)", Filter = "Text|*.txt|All|*.*" };
                if (dlg.ShowDialog() == true)
                {
                    TxtShipAddress.Text = $"Dosyadan seçildi: {System.IO.Path.GetFileName(dlg.FileName)}";
                }
            }
            catch { }
        }
        private void ReturnAddress_Select_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Adres listesi (örnek)", Filter = "Text|*.txt|All|*.*" };
                if (dlg.ShowDialog() == true)
                {
                    TxtReturnAddress.Text = $"Dosyadan seçildi: {System.IO.Path.GetFileName(dlg.FileName)}";
                }
            }
            catch { }
        }

        private void AppendAudit(string msg)
        {
            try
            {
                AuditList.Items.Insert(0, new { Time = DateTime.Now.ToString("HH:mm:ss"), Event = "INFO", Message = msg });
            }
            catch { }
        }

        // HTML önizleme
        private void ChkHtmlPreview_Checked(object sender, RoutedEventArgs e)
        {
            DescPreview.Visibility = Visibility.Visible;
            if (ChkMarkdownPreview?.IsChecked == true) UpdateMarkdownPreview(); else UpdateHtmlPreview();
        }
        private void ChkHtmlPreview_Unchecked(object sender, RoutedEventArgs e)
        {
            DescPreview.Visibility = Visibility.Collapsed;
        }
        private void TxtDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DescPreview.Visibility == Visibility.Visible)
            {
                if (ChkMarkdownPreview?.IsChecked == true) UpdateMarkdownPreview(); else UpdateHtmlPreview();
            }
        }
        private void UpdateHtmlPreview()
        {
            try
            {
                var html = $"<html><head><meta charset='utf-8'></head><body style='font-family:Segoe UI;font-size:14px;padding:8px'>{System.Net.WebUtility.HtmlEncode(TxtDescription.Text).Replace("\n", "<br/>")}</body></html>";
                DescPreview.NavigateToString(html);
            }
            catch { }
        }

        private void UpdateMarkdownPreview()
        {
            try
            {
                var src = TxtDescription.Text ?? string.Empty;
                string html = SimpleMarkdownToHtml(src);
                DescPreview.NavigateToString(html);
            }
            catch { }
        }

        private static string SimpleMarkdownToHtml(string markdown)
        {
            string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
            var lines = (markdown ?? string.Empty).Replace("\r", string.Empty).Split('\n');
            var htmlLines = new List<string>();
            bool inList = false;
            foreach (var raw in lines)
            {
                var line = raw;
                if (line.StartsWith("# ")) { if (inList) { htmlLines.Add("</ul>"); inList = false; } htmlLines.Add($"<h1>{Encode(line.Substring(2))}</h1>"); continue; }
                if (line.StartsWith("## ")) { if (inList) { htmlLines.Add("</ul>"); inList = false; } htmlLines.Add($"<h2>{Encode(line.Substring(3))}</h2>"); continue; }
                if (line.StartsWith("- "))
                {
                    if (!inList) { htmlLines.Add("<ul>"); inList = true; }
                    htmlLines.Add($"<li>{InlineMd(line.Substring(2))}</li>");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(line)) { if (inList) { htmlLines.Add("</ul>"); inList = false; } htmlLines.Add("<br/>"); continue; }
                if (inList) { htmlLines.Add("</ul>"); inList = false; }
                htmlLines.Add($"<p>{InlineMd(line)}</p>");
            }
            if (inList) htmlLines.Add("</ul>");
            var body = string.Join("\n", htmlLines);
            var css = "body{font-family:Segoe UI;font-size:14px;padding:8px} code{background:#f5f5f5;padding:2px 4px;border-radius:4px} pre{background:#f5f5f5;padding:8px;border-radius:6px}";
            return "<html><head><meta charset='utf-8'><style>" + css + "</style></head><body>" + body + "</body></html>";

            string InlineMd(string t)
            {
                // link
                t = System.Text.RegularExpressions.Regex.Replace(t, @"\[(.+?)\]\((.+?)\)", m => $"<a href='{Encode(m.Groups[2].Value)}' target='_blank'>{Encode(m.Groups[1].Value)}</a>");
                // bold ve italic
                t = System.Text.RegularExpressions.Regex.Replace(t, @"\*\*(.+?)\*\*", m => $"<strong>{Encode(m.Groups[1].Value)}</strong>");
                t = System.Text.RegularExpressions.Regex.Replace(t, @"\*(.+?)\*", m => $"<em>{Encode(m.Groups[1].Value)}</em>");
                return Encode(t);
            }
        }

        private void ChkMarkdownPreview_Checked(object sender, RoutedEventArgs e)
        {
            DescPreview.Visibility = Visibility.Visible;
            UpdateMarkdownPreview();
        }
        private void ChkMarkdownPreview_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ChkHtmlPreview?.IsChecked == true) UpdateHtmlPreview(); else DescPreview.Visibility = Visibility.Collapsed;
        }

        private void MakeCover_Click(object sender, RoutedEventArgs e)
        {
            var b = (sender as FrameworkElement);
            if (b?.DataContext is BitmapImage bi)
            {
                var idx = ImageList.Items.IndexOf(bi);
                if (idx >= 0) { _coverIndex = idx; CoverIndex = idx; }
                MesTechStok.Desktop.Utils.ToastManager.ShowInfo("Kapak görseli seçildi", "Görsel");
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("IMAGE", $"CoverSelected idx={idx}", nameof(ProductUploadPopup)); AppendAudit($"Kapak seçildi: {idx}"); } catch { }
            }
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            var b = (sender as FrameworkElement);
            if (b?.DataContext is BitmapImage bi)
            {
                var idx = ImageList.Items.IndexOf(bi);
                if (idx >= 0 && idx < _imageFiles.Count)
                {
                    _imageFiles.RemoveAt(idx);
                    ImageList.ItemsSource = null;
                    ImageList.ItemsSource = _imageFiles.Select(f => new BitmapImage(new Uri(f)));
                    try { NoImagesHint.Visibility = ImageList.HasItems ? Visibility.Collapsed : Visibility.Visible; } catch { }
                    try { ImageCountText.Text = $" ({_imageFiles.Count})"; } catch { }
                    if (_coverIndex == idx) _coverIndex = -1;
                    try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("IMAGE", $"Removed idx={idx}", nameof(ProductUploadPopup)); AppendAudit($"Görsel silindi: {idx}"); } catch { }
                }
            }
        }

        private async void GenerateColorSwatch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var colorText = (CmbColor.SelectedItem?.ToString() ?? "").Trim();
                if (string.IsNullOrWhiteSpace(colorText))
                {
                    MesTechStok.Desktop.Utils.ToastManager.ShowWarning("Önce bir renk seçin (ör. #FF0000)", "Renk Kartelası");
                    return;
                }
                Busy("Renk görseli oluşturuluyor…");
                var swatch = new ColorSwatchService();
                var file = await swatch.GenerateAsync(colorText, 512, colorText);
                _imageFiles.Add(file);
                ImageList.ItemsSource = null;
                ImageList.ItemsSource = _imageFiles.Select(f => new BitmapImage(new Uri(f)));
                MesTechStok.Desktop.Utils.ToastManager.ShowSuccess("Renk görseli eklendi", "Görsel");
                _isDirty = true;
            }
            catch (Exception ex)
            {
                MesTechStok.Desktop.Utils.ToastManager.ShowError($"Renk görseli üretimi hatası: {ex.Message}", "Görsel");
            }
            finally { Busy(); }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            var b = (sender as FrameworkElement);
            if (b?.DataContext is BitmapImage bi)
            {
                var idx = ImageList.Items.IndexOf(bi);
                if (idx > 0)
                {
                    var temp = _imageFiles[idx - 1];
                    _imageFiles[idx - 1] = _imageFiles[idx];
                    _imageFiles[idx] = temp;
                    ImageList.ItemsSource = null;
                    ImageList.ItemsSource = _imageFiles.Select(f => new BitmapImage(new Uri(f)));
                    try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("IMAGE", $"MovedUp from={idx} to={idx - 1}", nameof(ProductUploadPopup)); AppendAudit($"Görsel yukarı taşındı: {idx}→{idx - 1}"); } catch { }
                }
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            var b = (sender as FrameworkElement);
            if (b?.DataContext is BitmapImage bi)
            {
                var idx = ImageList.Items.IndexOf(bi);
                if (idx >= 0 && idx < _imageFiles.Count - 1)
                {
                    var temp = _imageFiles[idx + 1];
                    _imageFiles[idx + 1] = _imageFiles[idx];
                    _imageFiles[idx] = temp;
                    ImageList.ItemsSource = null;
                    ImageList.ItemsSource = _imageFiles.Select(f => new BitmapImage(new Uri(f)));
                    try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("IMAGE", $"MovedDown from={idx} to={idx + 1}", nameof(ProductUploadPopup)); AppendAudit($"Görsel aşağı taşındı: {idx}→{idx + 1}"); } catch { }
                }
            }
        }
        private void RemoveSelectedImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = ImageList.SelectedItems.Cast<BitmapImage>().ToList();
                if (selected.Count == 0) return;
                var removedCount = selected.Count;
                foreach (var bi in selected)
                {
                    var idx = ImageList.Items.IndexOf(bi);
                    if (idx >= 0 && idx < _imageFiles.Count) _imageFiles.RemoveAt(idx);
                }
                ImageList.ItemsSource = null;
                ImageList.ItemsSource = _imageFiles.Select(f => new BitmapImage(new Uri(f)));
                if (_coverIndex >= _imageFiles.Count) { _coverIndex = -1; CoverIndex = -1; }
                _isDirty = true;
                try { NoImagesHint.Visibility = ImageList.HasItems ? Visibility.Collapsed : Visibility.Visible; } catch { }
                try { ImageCountText.Text = $" ({_imageFiles.Count})"; } catch { }
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("IMAGE", $"RemovedSelected count={removedCount}", nameof(ProductUploadPopup)); AppendAudit($"Seçili görseller silindi: {removedCount}"); } catch { }
            }
            catch { }
        }

        private void ImageList_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.Up || e.Key == Key.Down))
                {
                    var sel = ImageList.SelectedItems.Cast<BitmapImage>().Select(x => ImageList.Items.IndexOf(x)).Where(i => i >= 0).OrderBy(i => i).ToList();
                    if (sel.Count == 0) return;
                    var coverPathBefore = (_coverIndex >= 0 && _coverIndex < _imageFiles.Count) ? _imageFiles[_coverIndex] : null;
                    if (e.Key == Key.Up)
                    {
                        if (sel.First() == 0) return;
                        var selSet = new HashSet<int>(sel);
                        for (int i = 0; i < _imageFiles.Count; i++)
                        {
                            if (selSet.Contains(i) && !selSet.Contains(i - 1) && i > 0)
                            {
                                var tmp = _imageFiles[i - 1];
                                _imageFiles[i - 1] = _imageFiles[i];
                                _imageFiles[i] = tmp;
                            }
                        }
                        // yeni indexler
                        sel = sel.Select(i => i - (i > 0 && !sel.Contains(i - 1) ? 1 : 0)).ToList();
                    }
                    else // Down
                    {
                        if (sel.Last() >= _imageFiles.Count - 1) return;
                        var selSet = new HashSet<int>(sel);
                        for (int i = _imageFiles.Count - 1; i >= 0; i--)
                        {
                            if (selSet.Contains(i) && !selSet.Contains(i + 1) && i < _imageFiles.Count - 1)
                            {
                                var tmp = _imageFiles[i + 1];
                                _imageFiles[i + 1] = _imageFiles[i];
                                _imageFiles[i] = tmp;
                            }
                        }
                        sel = sel.Select(i => i + (i < _imageFiles.Count - 1 && !sel.Contains(i + 1) ? 1 : 0)).ToList();
                    }
                    ImageList.ItemsSource = null;
                    ImageList.ItemsSource = _imageFiles.Select(f => new BitmapImage(new Uri(f)));
                    ImageList.SelectedItems.Clear();
                    foreach (var i in sel.Where(i => i >= 0 && i < ImageList.Items.Count))
                        ImageList.SelectedItems.Add(ImageList.Items[i]);
                    // Kapak güncelle
                    if (!string.IsNullOrWhiteSpace(coverPathBefore))
                    {
                        var newIdx = _imageFiles.FindIndex(p => string.Equals(p, coverPathBefore, StringComparison.OrdinalIgnoreCase));
                        if (newIdx >= 0) { _coverIndex = newIdx; CoverIndex = newIdx; }
                    }
                    _isDirty = true;
                    try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("IMAGE", $"KeyReorder dir={(e.Key == Key.Up ? "Up" : "Down")} count={sel.Count}", nameof(ProductUploadPopup)); AppendAudit($"Kısayol ile sıralama: {(e.Key == Key.Up ? "Yukarı" : "Aşağı")} ({sel.Count})"); } catch { }
                    e.Handled = true;
                }
            }
            catch { }
        }

        private void MakeSelectedCover_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var bi = ImageList.SelectedItem as BitmapImage;
                if (bi == null) return;
                var idx = ImageList.Items.IndexOf(bi);
                if (idx >= 0) { _coverIndex = idx; CoverIndex = idx; MesTechStok.Desktop.Utils.ToastManager.ShowInfo("Kapak görseli seçildi", "Görsel"); _isDirty = true; }
            }
            catch { }
        }

        // Yardımcı: farklı yol formatlarından güvenle BitmapImage üret
        private IEnumerable<BitmapImage> ToBitmapList(IEnumerable<string> paths)
        {
            foreach (var p in paths)
            {
                var bi = TryCreateBitmap(p);
                if (bi != null) yield return bi;
            }
        }

        private BitmapImage? TryCreateBitmap(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return null;

                Console.WriteLine($"[DEBUG] TryCreateBitmap - Processing path: '{path}'");
                Uri? uri = null;

                if (Uri.TryCreate(path, UriKind.Absolute, out var abs))
                {
                    uri = abs;
                    Console.WriteLine($"[DEBUG] Created absolute URI: {uri}");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Path is not absolute, trying to resolve...");

                    // Göreli yol ise uygulama diziniyle birleştir
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var combined = System.IO.Path.Combine(baseDir, path);
                    if (System.IO.File.Exists(combined))
                    {
                        uri = new Uri(combined, UriKind.Absolute);
                        Console.WriteLine($"[DEBUG] Found file in base directory: {combined}");
                    }
                    else if (_editingProductId.HasValue)
                    {
                        // Ürün klasörü altında olabilir
                        try
                        {
                            var storage = new ImageStorageService();
                            var pf = storage.GetProductFolder(_editingProductId.Value);
                            var inProd = System.IO.Path.Combine(pf, path);
                            if (System.IO.File.Exists(inProd))
                            {
                                uri = new Uri(inProd, UriKind.Absolute);
                                Console.WriteLine($"[DEBUG] Found file in product folder: {inProd}");
                            }
                            else
                            {
                                Console.WriteLine($"[DEBUG] File not found in product folder: {inProd}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DEBUG] Error checking product folder: {ex.Message}");
                        }
                    }

                    // Son çare: direct file path olarak dene
                    if (uri == null && System.IO.File.Exists(path))
                    {
                        uri = new Uri(System.IO.Path.GetFullPath(path), UriKind.Absolute);
                        Console.WriteLine($"[DEBUG] Using direct file path: {path}");
                    }
                }

                if (uri == null)
                {
                    Console.WriteLine($"[DEBUG] Could not resolve URI for path: {path}");
                    return null;
                }

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = uri;
                bmp.EndInit();
                bmp.Freeze();

                Console.WriteLine($"[DEBUG] Successfully created BitmapImage for: {uri}");
                return bmp;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] TryCreateBitmap failed for '{path}': {ex.Message}");
                return null;
            }
        }
    }
}
