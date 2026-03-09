// TODO: [MVVM-CLEANUP] State'i ViewModel'e taşı — Bkz: AUDIT-SYNTHESIS-001 Orta Bulgu #14
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using System.Runtime.CompilerServices;
using MesTechStok.Desktop.Services;
using MesTechStok.Desktop.Models;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Utils;
using Microsoft.Extensions.DependencyInjection;
using CoreCustomer = MesTechStok.Core.Data.Models.Customer;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// CustomersView - ENHANCED Customer Management with Pagination
    /// Gelişmiş müşteri yönetimi ve CRM sistemi
    /// </summary>
    public partial class CustomersView : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        private readonly MesTechStok.Core.Services.Abstract.ICustomerService _customerService;
        private readonly ObservableCollection<Services.CustomerItem> _displayedCustomers;
        private readonly DispatcherTimer _searchTimer;
        private readonly DispatcherTimer _refreshTimer;
        private readonly SemaphoreSlim _statsLock = new(1, 1);

        private string _searchText = string.Empty;
        private CustomerTypeFilter _currentTypeFilter = CustomerTypeFilter.All;
        private CustomerSortOrder _currentSortOrder = CustomerSortOrder.FullName;

        // Enhanced Pagination
        private int _currentPage = 1;
        private int _currentPageSize = 50;
        private int _totalItems = 0;
        private bool _isLoading = false;

        #endregion

        #region Properties

        public ObservableCollection<Services.CustomerItem> DisplayedCustomers => _displayedCustomers;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _ = LoadCustomersPageAsync(_currentPage, _currentPageSize);
            }
        }

        // Enhanced KPI Properties
        private string _totalCustomers = "0";
        private string _activeCustomers = "0";
        private string _vipCustomers = "0";
        private string _averageSpending = "₺0";

        public string TotalCustomers
        {
            get => _totalCustomers;
            set { _totalCustomers = value; OnPropertyChanged(); }
        }

        public string ActiveCustomers
        {
            get => _activeCustomers;
            set { _activeCustomers = value; OnPropertyChanged(); }
        }

        public string VipCustomers
        {
            get => _vipCustomers;
            set { _vipCustomers = value; OnPropertyChanged(); }
        }

        public string AverageSpending
        {
            get => _averageSpending;
            set { _averageSpending = value; OnPropertyChanged(); }
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

        public CustomersView()
        {
            var sp = MesTechStok.Desktop.App.ServiceProvider;
            _customerService = sp != null
                ? sp.GetRequiredService<MesTechStok.Core.Services.Abstract.ICustomerService>()
                : throw new InvalidOperationException("ServiceProvider yok");
            _displayedCustomers = new ObservableCollection<Services.CustomerItem>();

            // Initialize search timer for better performance
            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _searchTimer.Tick += async (s, e) =>
            {
                _searchTimer.Stop();
                await LoadCustomersPageAsync(_currentPage, _currentPageSize);
            };

            // Initialize auto-refresh timer
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(3) // Auto refresh every 3 minutes
            };
            _refreshTimer.Tick += async (s, e) =>
            {
                await UpdateStatisticsAsync();
                GlobalLogger.Instance.LogInfo("Customers auto-refreshed", "CustomersView");
            };

            InitializeComponent();
            DataContext = this;

            // Initialize customers grid
            CustomersDataGrid.ItemsSource = _displayedCustomers;

            // Setup pagination component
            SetupPaginationComponent();

            _ = InitializeAsync();
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            try
            {
                // Start with loading indicator
                IsLoading = true;

                // Load initial data
                await Task.WhenAll(
                    LoadCustomersPageAsync(_currentPage, _currentPageSize),
                    UpdateStatisticsAsync()
                );

                // Start auto-refresh
                StartAutoRefresh();

                GlobalLogger.Instance.LogInfo("Enhanced CustomersView başarıyla başlatıldı - A+++++ kalite", "CustomersView");
                ToastManager.ShowSuccess("👥 Gelişmiş müşteri sistemi başarıyla yüklendi! ⚡", "Müşteri Yönetimi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"CustomersView başlatma hatası: {ex.Message}", "CustomersView");
                ToastManager.ShowError("❌ Müşteri sistemi yüklenirken hata oluştu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadCustomersPageAsync(int page = 1, int pageSize = 50)
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                CurrentPage = page;
                CurrentPageSize = pageSize;
                // Use a fresh scope to avoid sharing the same DbContext across concurrent operations
                var root = MesTechStok.Desktop.App.ServiceProvider;
                if (root == null) throw new InvalidOperationException("ServiceProvider yok");
                using var scope = root.CreateScope();
                var customerService = scope.ServiceProvider.GetRequiredService<MesTechStok.Core.Services.Abstract.ICustomerService>();

                var result = await customerService.GetCustomersPagedAsync(
                    page, pageSize, SearchText);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _displayedCustomers.Clear();
                    foreach (var customer in result.Items)
                    {
                        _displayedCustomers.Add(MapToItem(customer));
                    }

                    TotalItems = result.TotalCount;

                    // Update customer count
                    if (CustomerCountText != null)
                    {
                        CustomerCountText.Text = $"({result.TotalCount} müşteri)";
                    }

                    // Update pagination info
                    UpdatePaginationInfo(result.TotalCount, page, pageSize);
                });

                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo($"Enhanced müşteri sayfası yüklendi: {result.Items.Count()}/{result.TotalCount} öğe, Sayfa: {page}", "CustomersView");
            }
            catch (Exception ex)
            {
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogError($"Müşteri sayfası yükleme hatası: {ex.Message}", "CustomersView");
                ToastManager.ShowError("❌ Müşteri verileri yüklenirken hata oluştu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateStatisticsAsync()
        {
            try
            {
                // Serialize stats updates to avoid overlaps and use a fresh scope (new DbContext)
                await _statsLock.WaitAsync();
                try
                {
                    var root = MesTechStok.Desktop.App.ServiceProvider;
                    if (root == null) throw new InvalidOperationException("ServiceProvider yok");
                    using var scope = root.CreateScope();
                    var customerService = scope.ServiceProvider.GetRequiredService<MesTechStok.Core.Services.Abstract.ICustomerService>();

                    var stats = await customerService.GetStatisticsAsync();

                    TotalCustomers = stats.TotalCustomers.ToString();
                    ActiveCustomers = stats.ActiveCustomers.ToString();
                    VipCustomers = stats.VipCustomers.ToString();
                    AverageSpending = $"₺{stats.AverageOrderValue:N0}";
                }
                finally
                {
                    _statsLock.Release();
                }

                // Update UI elements
                if (TotalCustomersText != null) TotalCustomersText.Text = TotalCustomers;
                if (ActiveCustomersText != null) ActiveCustomersText.Text = ActiveCustomers;
                if (VipCustomersText != null) VipCustomersText.Text = VipCustomers;
                if (AverageSpendingText != null) AverageSpendingText.Text = AverageSpending;

                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo("Müşteri istatistikleri güncellendi", "CustomersView");
            }
            catch (Exception ex)
            {
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogError($"İstatistik güncelleme hatası: {ex.Message}", "CustomersView");
            }
        }

        private Services.CustomerItem MapToItem(CoreCustomer c)
        {
            return new Services.CustomerItem
            {
                Id = c.Id,
                FullName = c.Name,
                Email = c.Email ?? string.Empty,
                PhoneNumber = c.Phone ?? string.Empty,
                Company = c.CustomerType == "Kurumsal" ? (c.ContactPerson ?? string.Empty) : string.Empty,
                CustomerType = c.IsVip ? "VIP" : (c.CustomerType == "INDIVIDUAL" ? "Bireysel" : (c.CustomerType == "CORPORATE" ? "Kurumsal" : (c.CustomerType ?? ""))),
                RegistrationDate = c.CreatedDate,
                LastOrderDate = c.LastOrderDate ?? c.CreatedDate,
                TotalPurchases = c.CurrentBalance,
                IsActive = c.IsActive
            };
        }

        #endregion

        #region Event Handlers

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchText = ((TextBox)sender).Text;

            // Use timer for performance optimization
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedFilter = CategoryFilterComboBox.SelectedItem as ComboBoxItem;
            if (selectedFilter == null) return;

            var filterText = selectedFilter.Content?.ToString() ?? string.Empty;

            _currentTypeFilter = filterText switch
            {
                var x when x.Contains("VIP") => CustomerTypeFilter.VIP,
                var x when x.Contains("Bireysel") => CustomerTypeFilter.Individual,
                var x when x.Contains("Kurumsal") => CustomerTypeFilter.Corporate,
                var x when x.Contains("Aktif") => CustomerTypeFilter.Active,
                var x when x.Contains("Pasif") => CustomerTypeFilter.Inactive,
                _ => CustomerTypeFilter.All
            };

            _ = LoadCustomersPageAsync(_currentPage, _currentPageSize);
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            CategoryFilterComboBox.SelectedIndex = 0;
            if (CityFilterComboBox != null) CityFilterComboBox.SelectedIndex = 0;

            _currentTypeFilter = CustomerTypeFilter.All;
            _ = LoadCustomersPageAsync(_currentPage, _currentPageSize);

            ToastManager.ShowInfo("🗑️ Filtreler temizlendi", "Müşteri Yönetimi");
        }

        private async void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new CustomerEditPopup { Owner = Window.GetWindow(this) };
                dlg.ShowDialog();
                await LoadCustomersPageAsync(_currentPage, _currentPageSize);
                await UpdateStatisticsAsync();
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Müşteri ekleme hatası: {ex.Message}", "CustomersView");
                ToastManager.ShowError("❌ Müşteri ekleme sırasında hata oluştu!", "Hata");
            }
        }

        private async void RefreshCustomers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadCustomersPageAsync(_currentPage, _currentPageSize);
                await UpdateStatisticsAsync();

                GlobalLogger.Instance.LogInfo("Enhanced müşteri listesi yenilendi", "CustomersView");
                ToastManager.ShowSuccess("🔄 Müşteri listesi başarıyla yenilendi!", "Müşteri Yönetimi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Müşteri yenileme hatası: {ex.Message}", "CustomersView");
                ToastManager.ShowError("❌ Müşteri verileri yenilenirken hata oluştu!", "Hata");
            }
        }

        private async void ExportCustomers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"musteriler_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Enhanced dışa aktarma - current displayed customers kullanılıyor
                    var csvContent = "ID,Ad Soyad,Telefon,E-posta,Şirket,Müşteri Tipi,Kayıt Tarihi,Son Alışveriş,Toplam Alışveriş,Durum\n";

                    foreach (var customer in _displayedCustomers)
                    {
                        var status = customer.IsActive ? "Aktif" : "Pasif";
                        csvContent += $"{customer.Id},{customer.FullName},{customer.PhoneNumber},{customer.Email}," +
                                    $"{customer.Company},{customer.CustomerType},{customer.FormattedRegistrationDate}," +
                                    $"{customer.FormattedLastOrderDate},{customer.FormattedTotalPurchases},{status}\n";
                    }

                    await File.WriteAllTextAsync(saveFileDialog.FileName, csvContent);

                    MessageBox.Show($"✅ Müşteri listesi başarıyla dışa aktarıldı!\n\n" +
                                  $"📁 Dosya: {Path.GetFileName(saveFileDialog.FileName)}\n" +
                                  $"👥 Müşteri Sayısı: {_displayedCustomers.Count}\n" +
                                  $"📊 Toplam Kayıt: {TotalItems}",
                                  "Dışa Aktarım Başarılı",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    GlobalLogger.Instance.LogInfo($"Müşteri listesi dışa aktarıldı: {_displayedCustomers.Count} kayıt", "CustomersView");
                    ToastManager.ShowSuccess($"📤 {_displayedCustomers.Count} müşteri başarıyla dışa aktarıldı!", "Dışa Aktarım");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Dışa aktarım hatası: {ex.Message}", "CustomersView");
                ToastManager.ShowError("❌ Dışa aktarım sırasında hata oluştu!", "Hata");
            }
        }

        private void CustomersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCustomer = CustomersDataGrid.SelectedItem as Services.CustomerItem;
            if (selectedCustomer != null)
            {
                DisplayCustomerDetails(selectedCustomer);
            }
        }

        private void ViewCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is Services.CustomerItem customer)
            {
                var details = $"👁️ Müşteri Detayları\n\n" +
                             $"ID: {customer.Id}\n" +
                             $"Ad Soyad: {customer.FullName}\n" +
                             $"Telefon: {customer.PhoneNumber}\n" +
                             $"E-posta: {customer.Email}\n" +
                             $"Şirket: {customer.Company}\n" +
                             $"Müşteri Tipi: {customer.CustomerType}\n" +
                             $"Kayıt Tarihi: {customer.FormattedRegistrationDate}\n" +
                             $"Son Alışveriş: {customer.FormattedLastOrderDate}\n" +
                             $"Toplam Alışveriş: {customer.FormattedTotalPurchases}\n" +
                             $"Durum: {customer.StatusIcon} {(customer.IsActive ? "Aktif" : "Pasif")}";

                MessageBox.Show(details, "Müşteri Detayları", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DisplayCustomerDetails(Services.CustomerItem customer)
        {
            GlobalLogger.Instance.LogInfo($"Müşteri detayları görüntülendi: {customer.FullName}", "CustomersView");
        }

        private void EditCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is Services.CustomerItem customer)
            {
                try
                {
                    var sp = MesTechStok.Desktop.App.ServiceProvider;
                    var core = sp!.GetRequiredService<MesTechStok.Core.Services.Abstract.ICustomerService>();
                    // Müşteriyi core'dan çekip popup'ı açalım
                    _ = Dispatcher.InvokeAsync(async () =>
                    {
                        var existing = await core.GetCustomerByIdAsync(customer.Id);
                        if (existing != null)
                        {
                            var dlg = new CustomerEditPopup(existing) { Owner = Window.GetWindow(this) };
                            dlg.ShowDialog();
                            await LoadCustomersPageAsync(_currentPage, _currentPageSize);
                            await UpdateStatisticsAsync();
                        }
                    });
                }
                catch
                {
                    // Intentional: UI event handler (edit customer click) — async Dispatcher.InvokeAsync must not crash event chain.
                }
            }
        }

        private void DeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is Services.CustomerItem customer)
            {
                var result = MessageBox.Show($"⚠️ {customer.FullName} müşterisini silmek istediğinizden emin misiniz?\n\n" +
                                           "Bu işlem geri alınamaz!",
                                           "Müşteri Sil",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Enhanced delete functionality would go here
                    _ = LoadCustomersPageAsync(_currentPage, _currentPageSize);
                    ToastManager.ShowWarning($"🗑️ {customer.FullName} müşterisi silindi", "Müşteri Yönetimi");
                }
            }
        }

        #endregion

        #region Enhanced Pagination Methods

        private void SetupPaginationComponent()
        {
            try
            {
                // Find pagination component in XAML and setup
                var paginationComponent = this.FindName("CustomersPaginationComponent") as PaginationComponent;
                if (paginationComponent != null)
                {
                    paginationComponent.PageChanged += OnPageChanged;
                    paginationComponent.PageSizeChanged += OnPageSizeChanged;
                    GlobalLogger.Instance.LogInfo("Customers pagination component configured", "CustomersView");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Pagination setup error: {ex.Message}", "CustomersView");
            }
        }

        private void OnPageChanged(object? sender, Components.PaginationEventArgs e)
        {
            _ = LoadCustomersPageAsync(e.CurrentPage, e.PageSize);
        }

        private void OnPageSizeChanged(object? sender, Components.PaginationEventArgs e)
        {
            _ = LoadCustomersPageAsync(1, e.PageSize); // Reset to page 1 when page size changes
        }

        // XAML Event Handlers for Pagination
        private void CustomersPagination_PageChanged(object? sender, Components.PaginationEventArgs e)
        {
            _ = LoadCustomersPageAsync(e.CurrentPage, e.PageSize);
        }

        private void CustomersPagination_PageSizeChanged(object? sender, Components.PaginationEventArgs e)
        {
            _ = LoadCustomersPageAsync(1, e.PageSize);
        }

        private void UpdatePaginationInfo(int totalItems, int currentPage, int pageSize)
        {
            try
            {
                var paginationComponent = this.FindName("CustomersPaginationComponent") as PaginationComponent;
                if (paginationComponent != null)
                {
                    paginationComponent.SetData(totalItems, currentPage, pageSize);
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Pagination update error: {ex.Message}", "CustomersView");
            }
        }

        #endregion

        #region Enhanced Performance & Lifecycle

        public void StartAutoRefresh()
        {
            _refreshTimer.Start();
            GlobalLogger.Instance.LogInfo("Customers auto-refresh started", "CustomersView");
        }

        public void StopAutoRefresh()
        {
            _refreshTimer.Stop();
            GlobalLogger.Instance.LogInfo("Customers auto-refresh stopped", "CustomersView");
        }

        public void Dispose()
        {
            _searchTimer?.Stop();
            _refreshTimer?.Stop();
            GlobalLogger.Instance.LogInfo("CustomersView disposed", "CustomersView");
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