// TODO: [MVVM-CLEANUP] State'i ViewModel'e taşı — Bkz: AUDIT-SYNTHESIS-001 Orta Bulgu #14
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Desktop.Services;
using MesTechStok.Desktop.Models;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Desktop.Models;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Utils;
using MesTechStok.Core.Services.Abstract;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// InventoryView - ENHANCED BRAVO TİMİ Real-time Stock Management with Pagination
    /// Gelişmiş gerçek zamanlı stok takip ve yönetim arayüzü
    /// </summary>
    public partial class InventoryView : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        private readonly MesTechStok.Desktop.Services.IInventoryDataService _inventoryService;
        private readonly ObservableCollection<MesTechStok.Desktop.Services.InventoryItem> _displayedInventory;
        private string _searchText = string.Empty;
        private string _scannedBarcode = string.Empty;
        private Services.StockStatusFilter _currentStatusFilter = Services.StockStatusFilter.All;
        private Services.InventorySortOrder _currentSortOrder = Services.InventorySortOrder.ProductName;

        // Authorization flags
        public bool CanAddStock { get; private set; } = true;
        public bool CanRemoveStock { get; private set; } = true;
        public bool CanTransferStock { get; private set; } = true;
        public bool CanExportInventory { get; private set; } = true;

        #endregion

        #region Properties

        public ObservableCollection<MesTechStok.Desktop.Services.InventoryItem> DisplayedInventory => _displayedInventory;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _ = LoadInventoryPageAsync();
            }
        }

        public string ScannedBarcode
        {
            get => _scannedBarcode;
            set
            {
                _scannedBarcode = value;
                OnPropertyChanged();
                _ = ProcessScannedBarcodeAsync();
            }
        }

        // Enhanced KPI Properties
        private string _totalInventoryValue = "₺0";
        private string _lowStockCount = "0";
        private string _todayMovements = "0";
        private string _stockAccuracy = "0%";

        public string TotalInventoryValue
        {
            get => _totalInventoryValue;
            set { _totalInventoryValue = value; OnPropertyChanged(); }
        }

        public string LowStockCount
        {
            get => _lowStockCount;
            set { _lowStockCount = value; OnPropertyChanged(); }
        }

        public string TodayMovements
        {
            get => _todayMovements;
            set { _todayMovements = value; OnPropertyChanged(); }
        }

        public string StockAccuracy
        {
            get => _stockAccuracy;
            set { _stockAccuracy = value; OnPropertyChanged(); }
        }

        #endregion

        #region Constructor

        public InventoryView()
        {
            // SQL tabanlı servis
            _inventoryService = new MesTechStok.Desktop.Services.SqlBackedInventoryService(
                MesTechStok.Desktop.App.ServiceProvider!.GetRequiredService<MesTechStok.Core.Data.AppDbContext>()
            );
            _displayedInventory = new ObservableCollection<MesTechStok.Desktop.Services.InventoryItem>();

            InitializeComponent();
            DataContext = this;

            // Initialize pagination component
            InventoryDataGrid.ItemsSource = _displayedInventory;

            _ = InitializeAsync();

            // Global log: stok görünümü açıldı
            GlobalLogger.Instance.LogInfo("Stok paneli açıldı", "InventoryView");
        }

        #endregion

        #region Event Handlers

        private async void RefreshInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Refresh all data
                await LoadInventoryPageAsync();
                await UpdateStatisticsAsync();
                await LoadRecentMovementsAsync();

                // Log action
                GlobalLogger.Instance.LogInfo("Enhanced stok listesi yenilendi", "InventoryView");

                // Show success toast
                ToastManager.ShowSuccess("🔄 Stok listesi başarıyla yenilendi!", "Stok Takip");

                // Show success animation
                if (sender is Button button)
                {
                    AnimateRefreshButton(button);
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Stok yenileme hatası: {ex.Message}", "InventoryView");
                ToastManager.ShowError("❌ Stok verileri yenilenirken hata oluştu!", "Hata");
            }
        }

        private async void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
                // Şu anda tüm kullanıcılar stok export edebilir
                GlobalLogger.Instance.LogInfo("Stok raporu CSV dışa aktarma başlatıldı", "InventoryView");
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Dosyası (*.csv)|*.csv",
                    FileName = $"stok_listesi_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() == true)
                {
                    var lines = new List<string> { "Barkod;Ürün Adı;Kategori;Mevcut Stok;Min Stok;Lokasyon;Fiyat" };
                    foreach (var item in _displayedInventory)
                    {
                        var line = string.Join(';', new string[]
                        {
                            item.Barcode,
                            item.ProductName.Replace(';', ','),
                            (item.Category ?? string.Empty).Replace(';', ','),
                            item.Stock.ToString(),
                            item.MinimumStock.ToString(),
                            (item.Location ?? string.Empty).Replace(';', ','),
                            item.Price.ToString("F2")
                        });
                        lines.Add(line);
                    }
                    System.IO.File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
                    ToastManager.ShowSuccess("📤 Stok listesi CSV olarak kaydedildi", "Dışa Aktarım");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Stok raporu dışa aktarma hatası: {ex.Message}", "InventoryView");
                ToastManager.ShowError("❌ CSV dışa aktarma sırasında hata oluştu", "Hata");
            }
        }

        private void InventorySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchText = ((TextBox)sender).Text;
        }

        private void StockStatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            FilterInventoryByStatus();
        }

        private void ScanBarcode_Click(object sender, RoutedEventArgs e)
        {
            // Activate barcode scanner
            BarcodeInputBox?.Focus();
            if (ScannerStatusText != null)
            {
                ScannerStatusText.Text = "Tarama başlatıldı...";
                var brush = FindResource("WarningOrangeBrush") as System.Windows.Media.Brush;
                if (brush != null) ScannerStatusText.Foreground = brush;
            }
        }

        private void BarcodeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            ScannedBarcode = ((TextBox)sender).Text;
        }

        private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessBarcodeEntry();
                e.Handled = true;
            }
        }

        private void InventoryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = InventoryDataGrid.SelectedItem as Services.InventoryItem;
            if (selectedItem != null)
            {
                DisplaySelectedItemDetails(selectedItem);
            }
        }

        // Quick Action Handlers
        private async void AddStock_Click(object sender, RoutedEventArgs e)
        {
            // Open Add Stock dialog
            // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
            // Şu anda tüm kullanıcılar stok ekleyebilir
            var selected = InventoryDataGrid.SelectedItem as Services.InventoryItem;
            if (selected == null)
            {
                ToastManager.ShowWarning("Lütfen bir ürün seçin.", "Stok Girişi");
                return;
            }
            var dlg = new AddStockLotDialog(new ProductItem
            {
                Id = selected.Id,
                Name = selected.ProductName,
                Stock = selected.Stock,
                Price = selected.Price
            })
            { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var sp2 = MesTechStok.Desktop.App.ServiceProvider;
                    var dbInv = sp2?.GetService<MesTechStok.Core.Services.Abstract.IInventoryService>();
                    if (dbInv == null)
                    {
                        ToastManager.ShowError("Envanter servisi bulunamadı.", "Stok Girişi");
                        return;
                    }
                    await dbInv.AddStockWithLotAsync(selected.Id, dlg.Quantity, dlg.UnitCost, dlg.LotNumber, dlg.ExpiryDate, notes: dlg.Notes, ProcessedBy: "UI");
                    ToastManager.ShowSuccess($"{dlg.Quantity} adet eklendi (Lot: {dlg.LotNumber}).", "Stok Girişi");
                    await LoadInventoryPageAsync();
                    await LoadRecentMovementsAsync();
                }
                catch (Exception ex)
                {
                    GlobalLogger.Instance.LogError($"Stok girişi hatası: {ex.Message}", "InventoryView");
                    ToastManager.ShowError("Stok girişi başarısız.", "Stok Girişi");
                }
            }
        }

        private async void RemoveStock_Click(object sender, RoutedEventArgs e)
        {
            // Open Remove Stock dialog
            // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
            // Şu anda tüm kullanıcılar stok çıkarabilir
            var selected = InventoryDataGrid.SelectedItem as Services.InventoryItem;
            if (selected == null)
            {
                ToastManager.ShowWarning("Lütfen bir ürün seçin.", "Stok Çıkışı");
                return;
            }
            var result = MessageBox.Show("Stok çıkışında FEFO (erken SKT öncelik) uygulansın mı?", "Stok Çıkışı", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) return;
            var sp3 = MesTechStok.Desktop.App.ServiceProvider;
            var dbInv = sp3?.GetService<MesTechStok.Core.Services.Abstract.IInventoryService>();
            if (dbInv == null) { ToastManager.ShowError("Envanter servisi bulunamadı.", "Stok Çıkışı"); return; }

            // Basit miktar sorusu
            var qtyStr = Microsoft.VisualBasic.Interaction.InputBox("Çıkış miktarı:", "Stok Çıkışı", "1");
            if (!int.TryParse(qtyStr, out var qty) || qty <= 0) { ToastManager.ShowWarning("Geçersiz miktar.", "Stok Çıkışı"); return; }

            try
            {
                if (result == MessageBoxResult.Yes)
                {
                    var mv = await dbInv.RemoveStockFefoAsync(selected.Id, qty, ProcessedBy: "UI");
                    ToastManager.ShowSuccess($"FEFO ile {qty} adet çıkıldı.", "Stok Çıkışı");
                }
                else
                {
                    var mv = await dbInv.RemoveStockAsync(selected.Id, qty, ProcessedBy: "UI");
                    ToastManager.ShowSuccess($"{qty} adet çıkıldı.", "Stok Çıkışı");
                }
                await LoadInventoryPageAsync();
                await LoadRecentMovementsAsync();
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Stok çıkışı hatası: {ex.Message}", "InventoryView");
                ToastManager.ShowError("Stok çıkışı başarısız.", "Stok Çıkışı");
            }
        }

        private async void TransferStock_Click(object sender, RoutedEventArgs e)
        {
            // Open Transfer Stock dialog
            // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
            // Şu anda tüm kullanıcılar stok transfer edebilir
            GlobalLogger.Instance.LogInfo("Stok transfer ekranı açıldı", "InventoryView");
            ToastManager.ShowInfo("🔄 Stok transfer ekranı açılıyor...", "Stok Transferi");
        }

        private void StockCount_Click(object sender, RoutedEventArgs e)
        {
            // Open Stock Count dialog
            GlobalLogger.Instance.LogInfo("Stok sayım ekranı açıldı", "InventoryView");
            ToastManager.ShowInfo("📋 Stok sayım ekranı açılıyor...", "Stok Sayımı");
        }

        private void ManualScan_Click(object sender, RoutedEventArgs e)
        {
            // Activate manual scanning mode
            BarcodeInputBox?.Clear();
            BarcodeInputBox?.Focus();
        }

        // Donanım barkod dinleme (InventoryView)
        private async void ChkBarcodeListenInv_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var svc = sp?.GetService<MesTechStok.Desktop.Services.IBarcodeService>();
                if (svc == null)
                {
                    ToastManager.ShowWarning("Barkod servisi yok", "Barkod");
                    return;
                }
                svc.BarcodeScanned += Inventory_BarcodeScanned;
                if (!svc.IsConnected) await svc.ConnectAsync();
                await svc.StartScanningAsync();
                try { BarcodeListenDot.Fill = System.Windows.Media.Brushes.LimeGreen; BarcodeListenText.Text = "Aktif"; }
                catch { /* Intentional: UI status indicator update — element may not be found during rapid toggle. */ }
                ToastManager.ShowInfo("Barkod dinleme aktif", "Barkod");
            }
            catch
            {
                // Intentional: UI event handler (barcode listener enable) — async barcode service ops must not crash event chain.
            }
        }

        private async void ChkBarcodeListenInv_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var svc = sp?.GetService<MesTechStok.Desktop.Services.IBarcodeService>();
                if (svc == null) return;
                await svc.StopScanningAsync();
                svc.BarcodeScanned -= Inventory_BarcodeScanned;
                await svc.DisconnectAsync();
                try { BarcodeListenDot.Fill = System.Windows.Media.Brushes.Gray; BarcodeListenText.Text = "Kapalı"; }
                catch { /* Intentional: UI status indicator update — element may not be found during rapid toggle. */ }
                ToastManager.ShowInfo("Barkod dinleme kapalı", "Barkod");
            }
            catch
            {
                // Intentional: UI event handler (barcode listener disable) — async barcode service ops must not crash event chain.
            }
        }

        private async void Inventory_BarcodeScanned(object? sender, BarcodeScannedEventArgs e)
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    BarcodeInputBox.Text = e.Barcode;
                    ScannedBarcode = e.Barcode;
                    await ProcessScannedBarcodeAsync();
                });
            }
            catch
            {
                // Intentional: barcode scanned event callback — Dispatcher.InvokeAsync must not crash on window close.
            }
        }

        private void ScannerSettings_Click(object sender, RoutedEventArgs e)
        {
            // Open scanner settings
            MessageBox.Show("Barkod tarayıcı ayarları açılıyor...", "Tarayıcı Ayarları", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewAllMovements_Click(object sender, RoutedEventArgs e)
        {
            // Open full movement history
            MessageBox.Show("Tüm hareketler listesi açılıyor...", "Stok Hareketleri", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            try
            {
                await SetupAuthorizationsAsync();
                await LoadInventoryPageAsync();
                await UpdateStatisticsAsync();
                await LoadRecentMovementsAsync();

                GlobalLogger.Instance.LogInfo("Enhanced InventoryView başlatıldı", "InventoryView");
                ToastManager.ShowSuccess("📦 Stok sistemi başarıyla yüklendi!", "Stok Takip");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"InventoryView başlatma hatası: {ex.Message}", "InventoryView");
                ToastManager.ShowError("❌ Stok sistemi yüklenirken hata oluştu!", "Hata");
            }
        }

        private async Task SetupAuthorizationsAsync()
        {
            // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
            // Şu anda tüm kullanıcılar tüm işlemleri yapabilir
            CanAddStock = CanRemoveStock = CanTransferStock = CanExportInventory = true;
            OnPropertyChanged(nameof(CanAddStock));
            OnPropertyChanged(nameof(CanRemoveStock));
            OnPropertyChanged(nameof(CanTransferStock));
            OnPropertyChanged(nameof(CanExportInventory));
        }

        private async Task LoadInventoryPageAsync(int page = 1, int pageSize = 50)
        {
            try
            {
                var result = await _inventoryService.GetInventoryPagedAsync(
                    page, pageSize, SearchText, _currentStatusFilter, _currentSortOrder);

                _displayedInventory.Clear();
                foreach (var item in result.Items)
                {
                    _displayedInventory.Add(item);
                }

                // Update pagination info if pagination component exists
                // InventoryPagination.UpdatePagination(result.TotalItems, result.CurrentPage, result.PageSize);

                GlobalLogger.Instance.LogInfo($"Stok sayfası yüklendi: {result.Items.Count} öğe", "InventoryView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Stok sayfası yükleme hatası: {ex.Message}", "InventoryView");
                ToastManager.ShowError("❌ Stok verileri yüklenirken hata oluştu!", "Hata");
            }
        }

        private async Task UpdateStatisticsAsync()
        {
            try
            {
                var stats = await _inventoryService.GetInventoryStatisticsAsync();

                TotalInventoryValue = $"₺{stats.TotalInventoryValue:N2}";
                LowStockCount = stats.LowStockCount.ToString();
                TodayMovements = stats.TodayMovements.ToString();
                StockAccuracy = $"{stats.StockAccuracy:F1}%";

                GlobalLogger.Instance.LogInfo("Stok istatistikleri güncellendi", "InventoryView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"İstatistik güncelleme hatası: {ex.Message}", "InventoryView");
            }
        }

        private async Task LoadRecentMovementsAsync()
        {
            try
            {
                var movements = await _inventoryService.GetRecentMovementsAsync(10);

                if (RecentMovementsList != null)
                {
                    RecentMovementsList.ItemsSource = movements;
                }

                GlobalLogger.Instance.LogInfo($"{movements.Count} stok hareketi yüklendi", "InventoryView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Stok hareketleri yükleme hatası: {ex.Message}", "InventoryView");
            }
        }

        private async Task ProcessScannedBarcodeAsync()
        {
            if (string.IsNullOrEmpty(ScannedBarcode)) return;

            try
            {
                var foundItem = await _inventoryService.GetInventoryByBarcodeAsync(ScannedBarcode);
                if (foundItem != null)
                {
                    // Find and select the item in the grid
                    var displayItem = _displayedInventory.FirstOrDefault(i => i.Id == foundItem.Id);
                    if (displayItem != null)
                    {
                        InventoryDataGrid.SelectedItem = displayItem;
                        InventoryDataGrid.ScrollIntoView(displayItem);
                    }

                    if (ScannerStatusText != null)
                    {
                        ScannerStatusText.Text = "✅ Ürün bulundu!";
                        ScannerStatusText.Foreground = System.Windows.Media.Brushes.Green;
                    }

                    ToastManager.ShowSuccess($"📦 {foundItem.ProductName} bulundu!", "Barkod Tarama");
                }
                else
                {
                    if (ScannerStatusText != null)
                    {
                        ScannerStatusText.Text = "❌ Ürün bulunamadı!";
                        ScannerStatusText.Foreground = System.Windows.Media.Brushes.Red;
                    }

                    ToastManager.ShowWarning("Barkod bulunamadı!", "Barkod Tarama");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Barkod tarama hatası: {ex.Message}", "InventoryView");
                ToastManager.ShowError("❌ Barkod tarama hatası!", "Hata");
            }
        }

        private void ProcessBarcodeEntry()
        {
            _ = ProcessScannedBarcodeAsync();
            BarcodeInputBox?.Clear();
        }

        private void FilterInventoryByStatus()
        {
            var selectedFilter = StockStatusFilter.SelectedItem as ComboBoxItem;
            if (selectedFilter == null) return;

            var filterText = selectedFilter.Content?.ToString() ?? string.Empty;

            _currentStatusFilter = filterText switch
            {
                var x when x.Contains("Normal") => Services.StockStatusFilter.Normal,
                var x when x.Contains("Düşük") => Services.StockStatusFilter.Low,
                var x when x.Contains("Kritik") => Services.StockStatusFilter.Critical,
                var x when x.Contains("Stok Yok") => Services.StockStatusFilter.OutOfStock,
                _ => Services.StockStatusFilter.All
            };

            _ = LoadInventoryPageAsync();
        }

        private void DisplaySelectedItemDetails(Services.InventoryItem item)
        {
            // Enhanced item details display
            var details = $"Ürün: {item.ProductName}\n" +
                         $"Mevcut Stok: {item.Stock}\n" +
                         $"Minimum Stok: {item.MinimumStock}\n" +
                         $"Lokasyon: {item.Location}\n" +
                         $"Toplam Değer: {item.TotalValue:C}";

            MessageBox.Show(details, "Ürün Detayları", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AnimateRefreshButton(Button button)
        {
            if (button == null) return;

            try
            {
                var storyboard = FindResource("StockUpdateAnimation") as System.Windows.Media.Animation.Storyboard;
                storyboard?.Begin();
            }
            catch
            {
                // Animation resource not found, ignore
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    // Enhanced InventoryView using Services.InventoryItem and Services.StockMovement models
}