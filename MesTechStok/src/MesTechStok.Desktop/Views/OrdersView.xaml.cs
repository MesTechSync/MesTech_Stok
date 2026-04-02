// Debt: [MVVM-CLEANUP] State'i ViewModel'e tasi — Bkz: AUDIT-SYNTHESIS-001 Orta Bulgu #14
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MesTechStok.Desktop.Services;
using MesTechStok.Desktop.Models;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Utils;
using MesTechStok.Core.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable CS0618 // Core.Data.Models type alias — will migrate to Domain entities in H32
using CoreOrderStatus = MesTechStok.Core.Data.Models.OrderStatus;
#pragma warning restore CS0618

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// OrdersView - ENHANCED Order Management with Pagination
    /// Gelişmiş sipariş yönetimi ve takip sistemi
    /// </summary>
    public partial class OrdersView : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        // SQL destekli servis (in-memory yerine)
        private readonly MesTechStok.Core.Services.Abstract.IOrderService _orderService;
        private readonly ObservableCollection<Services.OrderItem> _displayedOrders;
        private readonly DispatcherTimer _searchTimer;
        private readonly DispatcherTimer _refreshTimer;

        private string _searchText = string.Empty;
        private bool _canCreateOrders;
        private bool _canEditOrders;
        private bool _canCancelOrders;
        private bool _canUpdateOrderStatus;
        private OrderStatusFilter _currentStatusFilter = OrderStatusFilter.All;
        private OrderSortOrder _currentSortOrder = OrderSortOrder.OrderDateDesc;

        // Enhanced Pagination
        private int _currentPage = 1;
        private int _currentPageSize = 50;
        private int _totalItems = 0;
        private bool _isLoading = false;

        #endregion

        #region Properties

        public ObservableCollection<Services.OrderItem> DisplayedOrders => _displayedOrders;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _ = LoadOrdersPageAsync(_currentPage, _currentPageSize);
            }
        }

        // Authorization-bound properties for XAML bindings
        public bool CanCreateOrders { get => _canCreateOrders; private set { _canCreateOrders = value; OnPropertyChanged(); } }
        public bool CanEditOrders { get => _canEditOrders; private set { _canEditOrders = value; OnPropertyChanged(); } }
        public bool CanCancelOrders { get => _canCancelOrders; private set { _canCancelOrders = value; OnPropertyChanged(); } }
        public bool CanUpdateOrderStatus { get => _canUpdateOrderStatus; private set { _canUpdateOrderStatus = value; OnPropertyChanged(); } }

        // Enhanced KPI Properties
        private string _totalOrders = "0";
        private string _pendingOrders = "0";
        private string _completedOrders = "0";
        private string _cancelledOrders = "0";
        private string _dailyRevenue = "₺0";

        public string TotalOrders
        {
            get => _totalOrders;
            set { _totalOrders = value; OnPropertyChanged(); }
        }

        public string PendingOrders
        {
            get => _pendingOrders;
            set { _pendingOrders = value; OnPropertyChanged(); }
        }

        public string CompletedOrders
        {
            get => _completedOrders;
            set { _completedOrders = value; OnPropertyChanged(); }
        }

        public string CancelledOrders
        {
            get => _cancelledOrders;
            set { _cancelledOrders = value; OnPropertyChanged(); }
        }

        public string DailyRevenue
        {
            get => _dailyRevenue;
            set { _dailyRevenue = value; OnPropertyChanged(); }
        }

        // Enhanced Pagination Properties
        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public int CurrentPageSize
        {
            get => _currentPageSize;
            set { _currentPageSize = value; OnPropertyChanged(); }
        }

        public int TotalItems
        {
            get => _totalItems;
            set { _totalItems = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();

                // Update loading UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (LoadingBorder != null)
                    {
                        LoadingBorder.Visibility = _isLoading ? Visibility.Visible : Visibility.Collapsed;
                    }
                });
            }
        }

        #endregion

        #region Constructor

        public OrdersView()
        {
            _orderService = MesTechStok.Desktop.App.Services!.GetRequiredService<MesTechStok.Core.Services.Abstract.IOrderService>();
            _displayedOrders = new ObservableCollection<Services.OrderItem>();

            // Initialize search timer for better performance
            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _searchTimer.Tick += async (s, e) =>
            {
                _searchTimer.Stop();
                await LoadOrdersPageAsync(_currentPage, _currentPageSize);
            };

            // Initialize auto-refresh timer
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(2) // Auto refresh every 2 minutes
            };
            _refreshTimer.Tick += async (s, e) =>
            {
                await UpdateStatisticsAsync();
                GlobalLogger.Instance.LogInfo("Orders auto-refreshed", "OrdersView");
            };

            InitializeComponent();
            DataContext = this;

            // Initialize orders grid
            OrdersDataGrid.ItemsSource = _displayedOrders;

            // Setup pagination component
            SetupPaginationComponent();

            _ = InitializeAsync();
        }

        #endregion

        #region Keyboard Shortcuts

        private void View_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
                { AddOrder_Click(this, new RoutedEventArgs()); e.Handled = true; }
                else if (e.Key == Key.F5 || (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control))
                { RefreshOrders_Click(this, new RoutedEventArgs()); e.Handled = true; }
                else if (e.Key == Key.Escape)
                { OrdersDataGrid.SelectedItem = null; e.Handled = true; }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(OrdersView)} KeyDown handler error: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private async void RefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadOrdersPageAsync(_currentPage, _currentPageSize);
                await UpdateStatisticsAsync();

                GlobalLogger.Instance.LogInfo("Enhanced sipariş listesi yenilendi", "OrdersView");
                ToastManager.ShowSuccess("🔄 Sipariş listesi başarıyla yenilendi!", "Sipariş Yönetimi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Sipariş yenileme hatası: {ex.Message}", "OrdersView");
                ToastManager.ShowError("❌ Sipariş verileri yenilenirken hata oluştu!", "Hata");
            }
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Security: SimpleSecurityService integration pending
                // Şu anda tüm kullanıcılar sipariş ekleyebilir
                GlobalLogger.Instance.LogInfo("Yeni sipariş ekleme ekranı açıldı", "OrdersView");
                ToastManager.ShowInfo("🛒 Yeni sipariş ekleme ekranı açılıyor...", "Sipariş Yönetimi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(OrdersView)} AddOrder handler error: {ex.Message}");
            }
        }

        private void ExportOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Security: SimpleSecurityService integration pending
                // Şu anda tüm kullanıcılar export yapabilir
                GlobalLogger.Instance.LogInfo("Sipariş dışa aktarma başlatıldı", "OrdersView");
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Dosyası (*.csv)|*.csv",
                    FileName = $"siparisler_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() == true)
                {
                    var lines = new List<string>();
                    lines.Add("Id;SiparisNo;Musteri;Tarih;Tutar;Durum");
                    foreach (var o in _displayedOrders)
                    {
                        var no = string.IsNullOrWhiteSpace(o.OrderNumber) ? o.Id.ToString() : o.OrderNumber;
                        lines.Add($"{o.Id};{no};{o.CustomerName};{o.FormattedDate};{o.TotalAmount:N2};{o.Status}");
                    }
                    System.IO.File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
                    ToastManager.ShowSuccess($"📤 Dışa aktarıldı: {sfd.FileName}", "Dışa Aktarım");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Dışa aktarma hatası: {ex.Message}", "OrdersView");
                ToastManager.ShowError("❌ Dışa aktarma başarısız!", "Hata");
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                SearchText = ((TextBox)sender).Text;

                // Use timer for performance optimization
                _searchTimer.Stop();
                _searchTimer.Start();
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(OrdersView)} SearchTextBox handler error: {ex.Message}");
            }
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedFilter = StatusFilterComboBox.SelectedItem as ComboBoxItem;
                if (selectedFilter == null) return;

                var filterText = selectedFilter.Content?.ToString() ?? string.Empty;

                _currentStatusFilter = filterText switch
                {
                    var x when x.Contains("Bekleyen") => OrderStatusFilter.Pending,
                    var x when x.Contains("Hazırlanıyor") => OrderStatusFilter.Processing,
                    var x when x.Contains("Teslim Edildi") => OrderStatusFilter.Completed,
                    var x when x.Contains("İptal Edildi") => OrderStatusFilter.Cancelled,
                    _ => OrderStatusFilter.All
                };

                _ = LoadOrdersPageAsync(_currentPage, _currentPageSize);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(OrdersView)} StatusFilter handler error: {ex.Message}");
            }
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            // Null güvenli temizleme
            try
            {
                SearchTextBox?.Clear();
                if (StatusFilterComboBox != null) StatusFilterComboBox.SelectedIndex = 0;
                if (PaymentFilterComboBox != null) PaymentFilterComboBox.SelectedIndex = 0;
                if (DateFilterComboBox != null) DateFilterComboBox.SelectedIndex = 0;
            }
            catch
            {
                // Intentional: UI event handler (filter clear) — UI elements may not be available during template loading.
            }

            _currentStatusFilter = OrderStatusFilter.All;
            _ = LoadOrdersPageAsync(_currentPage, _currentPageSize);

            ToastManager.ShowInfo("🗑️ Filtreler temizlendi", "Sipariş Yönetimi");
        }

        private void OrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedOrder = OrdersDataGrid?.SelectedItem as Services.OrderItem;
                if (selectedOrder != null)
                {
                    DisplayOrderDetails(selectedOrder);
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(OrdersView)} DataGrid SelectionChanged handler error: {ex.Message}");
            }
        }

        // Order action handlers
        private void ViewOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (((Button)sender).DataContext is Services.OrderItem order)
                {
                    var details = $"Sipariş No: {(string.IsNullOrWhiteSpace(order.OrderNumber) ? order.Id.ToString() : order.OrderNumber)}\n" +
                         $"Müşteri: {order.CustomerName}\n" +
                         $"Tarih: {order.FormattedDate}\n" +
                         $"Tutar: {order.FormattedAmount}\n" +
                         $"Durum: {order.Status}\n" +
                         $"Ürünler: {order.ProductsList}";

                    MessageBox.Show(details, "Sipariş Detayları", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(OrdersView)} ViewOrder handler error: {ex.Message}");
            }
        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToastManager.ShowInfo("✏️ Sipariş düzenleme ekranı açılıyor...", "Sipariş Yönetimi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(OrdersView)} EditOrder handler error: {ex.Message}");
            }
        }

        private async void UpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is Services.OrderItem order)
            {
                try
                {
                    // Security: SimpleSecurityService integration pending
                    // Şu anda tüm kullanıcılar sipariş durumu güncelleyebilir
                    // Cycle through statuses for demo
                    var newStatus = order.Status switch
                    {
                        Services.OrderStatus.Pending => Services.OrderStatus.Processing,
                        Services.OrderStatus.Processing => Services.OrderStatus.Completed,
                        Services.OrderStatus.Completed => Services.OrderStatus.Pending,
                        Services.OrderStatus.Cancelled => Services.OrderStatus.Pending,
                        _ => Services.OrderStatus.Pending
                    };

                    await _orderService.UpdateOrderStatusAsync(order.Id, MapToCoreStatus(newStatus));
                    await LoadOrdersPageAsync(_currentPage, _currentPageSize);

                    ToastManager.ShowSuccess($"📋 Sipariş durumu güncellendi: {newStatus}", "Sipariş Yönetimi");
                }
                catch (Exception ex)
                {
                    GlobalLogger.Instance.LogError($"Sipariş durumu güncelleme hatası: {ex.Message}", "OrdersView");
                    ToastManager.ShowError("❌ Sipariş durumu güncellenirken hata oluştu!", "Hata");
                }
            }
        }

        private void CancelOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (((Button)sender).DataContext is Services.OrderItem order)
                {
                    // Security: SimpleSecurityService integration pending
                    // Şu anda tüm kullanıcılar sipariş iptal edebilir
                    var result = MessageBox.Show($"'{order.CustomerName}' müşterisinin siparişini iptal etmek istediğinizden emin misiniz?",
                                               "Sipariş İptali", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _ = _orderService.UpdateOrderStatusAsync(order.Id, CoreOrderStatus.Cancelled);
                        _ = LoadOrdersPageAsync(_currentPage, _currentPageSize);
                        ToastManager.ShowWarning($"🗑️ Sipariş iptal edildi: #{order.Id}", "Sipariş Yönetimi");
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(OrdersView)} CancelOrder handler error: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            try
            {
                // Start with loading indicator
                ShowLoading();

                // Authorization setup
                await SetupAuthorizationsAsync();

                // Load initial data
                await Task.WhenAll(
                    LoadOrdersPageAsync(_currentPage, _currentPageSize),
                    UpdateStatisticsAsync()
                );

                // Check if there are no orders to show empty state
                if (_displayedOrders.Count == 0)
                {
                    ShowEmpty();
                }
                else
                {
                    HideAllStates();
                }

                // Start auto-refresh
                StartAutoRefresh();

                GlobalLogger.Instance.LogInfo("Enhanced OrdersView başarıyla başlatıldı - A+++++ kalite", "OrdersView");
                ToastManager.ShowSuccess("🛒 Gelişmiş sipariş sistemi başarıyla yüklendi! ⚡", "Sipariş Yönetimi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"OrdersView başlatma hatası: {ex.Message}", "OrdersView");
                ShowError($"Sipariş sistemi yüklenirken hata oluştu: {ex.Message}");
            }
        }

        private Task SetupAuthorizationsAsync()
        {
            // Security: SimpleSecurityService integration pending
            // Şu anda tüm kullanıcılar tüm işlemleri yapabilir
            CanCreateOrders = CanEditOrders = CanCancelOrders = CanUpdateOrderStatus = true;
            return Task.CompletedTask;
        }

        private async Task LoadOrdersPageAsync(int page = 1, int pageSize = 50)
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                CurrentPage = page;
                CurrentPageSize = pageSize;

                // Core IOrderService listeleme sayfalı API sağlamıyor; Desktop katmanında OrderItem projeksiyonu kullanılıyordu.
                // Geçici: Tüm siparişleri çekip basit sayfalama uygula (ileride Core’a sayfalı metot eklenecek)
                var all = await _orderService.GetAllOrdersAsync();
                var filtered = string.IsNullOrWhiteSpace(SearchText)
                    ? all
                    : all.Where(o => (o.CustomerName ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                     (o.OrderNumber ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                var totalItems = filtered.Count();
                var pageItems = filtered
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _displayedOrders.Clear();
                    foreach (var o in pageItems)
                    {
                        _displayedOrders.Add(new Services.OrderItem
                        {
                            Id = o.Id,
                            OrderNumber = o.OrderNumber ?? $"#{o.Id}",
                            CustomerName = o.CustomerName ?? string.Empty,
                            OrderDate = o.OrderDate,
                            TotalAmount = o.TotalAmount,
                            Status = MapToUiStatus(o.Status)
                        });
                    }

                    TotalItems = totalItems;

                    // Update order count
                    if (OrderCountText != null)
                    {
                        OrderCountText.Text = $"({totalItems} sipariş)";
                    }

                    // **CRITICAL FIX: Configure pagination component**
                    OrdersPaginationComponent?.SetData(
                        totalItems,
                        page,
                        pageSize);

                    // Store current pagination state

                    // Update pagination info
                    UpdatePaginationInfo(totalItems, page, pageSize);
                });

                GlobalLogger.Instance.LogInfo($"Enhanced sipariş sayfası yüklendi: {pageItems.Count}/{totalItems} öğe, Sayfa: {page}", "OrdersView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Sipariş sayfası yükleme hatası: {ex.Message}", "OrdersView");
                ShowError($"Sipariş verileri yüklenirken hata oluştu: {ex.Message}");
            }
        }

        private async Task UpdateStatisticsAsync()
        {
            try
            {
                // Geçici: Core servis istatistik API sağlamıyor, tümünü çekip hesapla
                var all = await _orderService.GetAllOrdersAsync();
                TotalOrders = all.Count.ToString();
                PendingOrders = all.Count(o => o.Status == CoreOrderStatus.Pending).ToString();
                CompletedOrders = all.Count(o => o.Status == CoreOrderStatus.Delivered).ToString();
                CancelledOrders = all.Count(o => o.Status == CoreOrderStatus.Cancelled).ToString();
                DailyRevenue = $"₺{(all.Any() ? all.Average(o => o.TotalAmount) : 0):N0}";

                // Update UI elements
                if (TotalOrdersText != null) TotalOrdersText.Text = TotalOrders;
                if (PendingOrdersText != null) PendingOrdersText.Text = PendingOrders;
                if (CompletedOrdersText != null) CompletedOrdersText.Text = CompletedOrders;
                if (CancelledOrdersText != null) CancelledOrdersText.Text = CancelledOrders;
                if (DailyRevenueText != null) DailyRevenueText.Text = DailyRevenue;

                GlobalLogger.Instance.LogInfo("Sipariş istatistikleri güncellendi", "OrdersView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"İstatistik güncelleme hatası: {ex.Message}", "OrdersView");
            }
        }

        private void DisplayOrderDetails(Services.OrderItem order)
        {
            // Enhanced order details display
            var details = $"Sipariş #{order.Id} Detayları:\n\n" +
                         $"Müşteri: {order.CustomerName}\n" +
                         $"Tarih: {order.FormattedDate}\n" +
                         $"Durum: {order.StatusIcon} {order.Status}\n" +
                         $"Tutar: {order.FormattedAmount}\n" +
                         $"Son Güncelleme: {order.FormattedLastUpdate}";

            // This could be enhanced with a proper detail panel
            GlobalLogger.Instance.LogInfo($"Sipariş detayları görüntülendi: #{order.Id}", "OrdersView");
        }

        private Services.OrderStatus MapToUiStatus(CoreOrderStatus status)
        {
            return status switch
            {
                CoreOrderStatus.Pending => Services.OrderStatus.Pending,
                CoreOrderStatus.Confirmed => Services.OrderStatus.Processing,
                CoreOrderStatus.Shipped => Services.OrderStatus.Completed,
                CoreOrderStatus.Delivered => Services.OrderStatus.Completed,
                CoreOrderStatus.Cancelled => Services.OrderStatus.Cancelled,
                _ => Services.OrderStatus.Pending
            };
        }

        #region Enhanced Pagination Methods

        private void SetupPaginationComponent()
        {
            try
            {
                // Find pagination component in XAML and setup
                var paginationComponent = this.FindName("OrdersPaginationComponent") as PaginationComponent;
                if (paginationComponent != null)
                {
                    paginationComponent.PageChanged += OnPageChanged;
                    paginationComponent.PageSizeChanged += OnPageSizeChanged;
                    GlobalLogger.Instance.LogInfo("Orders pagination component configured", "OrdersView");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Pagination setup error: {ex.Message}", "OrdersView");
            }
        }

        private void OnPageChanged(object? sender, Components.PaginationEventArgs e)
        {
            _ = LoadOrdersPageAsync(e.CurrentPage, e.PageSize);
        }

        private void OnPageSizeChanged(object? sender, Components.PaginationEventArgs e)
        {
            _ = LoadOrdersPageAsync(1, e.PageSize); // Reset to page 1 when page size changes
        }

        // XAML Event Handlers for Pagination
        private void OrdersPagination_PageChanged(object? sender, PaginationEventArgs e)
        {
            _ = LoadOrdersPageAsync(e.CurrentPage, e.PageSize);
        }

        private void OrdersPagination_PageSizeChanged(object? sender, PaginationEventArgs e)
        {
            _ = LoadOrdersPageAsync(1, e.PageSize);
        }

        private CoreOrderStatus MapToCoreStatus(Services.OrderStatus status)
        {
            return status switch
            {
                Services.OrderStatus.Pending => CoreOrderStatus.Pending,
                Services.OrderStatus.Processing => CoreOrderStatus.Confirmed,
                Services.OrderStatus.Completed => CoreOrderStatus.Delivered,
                Services.OrderStatus.Cancelled => CoreOrderStatus.Cancelled,
                _ => CoreOrderStatus.Pending
            };
        }

        private void UpdatePaginationInfo(int totalItems, int currentPage, int pageSize)
        {
            try
            {
                var paginationComponent = this.FindName("OrdersPaginationComponent") as PaginationComponent;
                if (paginationComponent != null)
                {
                    paginationComponent.SetData(totalItems, currentPage, pageSize);
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Pagination update error: {ex.Message}", "OrdersView");
            }
        }

        #endregion

        #region Enhanced Performance & Lifecycle

        public void StartAutoRefresh()
        {
            _refreshTimer.Start();
            GlobalLogger.Instance.LogInfo("Orders auto-refresh started", "OrdersView");
        }

        public void StopAutoRefresh()
        {
            _refreshTimer.Stop();
            GlobalLogger.Instance.LogInfo("Orders auto-refresh stopped", "OrdersView");
        }

        public void Dispose()
        {
            _searchTimer?.Stop();
            _refreshTimer?.Stop();
            GlobalLogger.Instance.LogInfo("OrdersView disposed", "OrdersView");
        }

        #endregion

        #endregion

        #region Loading/Empty/Error State Helpers

        private void ShowLoading()
        {
            IsLoading = true;
            OrdersEmptyState.Visibility = Visibility.Collapsed;
            OrdersErrorState.Visibility = Visibility.Collapsed;
        }

        private void ShowEmpty()
        {
            IsLoading = false;
            OrdersEmptyState.Visibility = Visibility.Visible;
            OrdersErrorState.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            IsLoading = false;
            OrdersEmptyState.Visibility = Visibility.Collapsed;
            OrdersErrorState.Visibility = Visibility.Visible;
            OrdersErrorText.Text = message;
        }

        private void HideAllStates()
        {
            IsLoading = false;
            OrdersEmptyState.Visibility = Visibility.Collapsed;
            OrdersErrorState.Visibility = Visibility.Collapsed;
        }

        private void RetryOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HideAllStates();
                _ = InitializeAsync();
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(OrdersView)} RetryOrders handler error: {ex.Message}");
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
}
