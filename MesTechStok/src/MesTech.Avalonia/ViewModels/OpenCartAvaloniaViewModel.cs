using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class OpenCartAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    // Connection / KPI
    [ObservableProperty] private bool isConnected;
    [ObservableProperty] private int productCount;
    [ObservableProperty] private int orderCount;
    [ObservableProperty] private decimal dailyRevenue;
    [ObservableProperty] private string syncStatus = "Bekliyor";
    [ObservableProperty] private string lastSyncTime = "-";
    [ObservableProperty] private int totalCount;

    // Store selector
    [ObservableProperty] private string selectedStore = "Demo Store 1";
    public ObservableCollection<string> Stores { get; } = ["Demo Store 1", "Demo Store 2"];

    // Active tab index (0=Ürünler, 1=Siparişler, 2=Kategoriler, 3=Ayarlar)
    [ObservableProperty] private int selectedTabIndex;

    // Tab 1 — Ürünler
    public ObservableCollection<OpenCartProductItem> Products { get; } = [];

    // Tab 2 — Siparişler
    public ObservableCollection<OpenCartOrderItem> Orders { get; } = [];

    // Tab 3 — Kategoriler (platform ↔ MesTech mapping)
    public ObservableCollection<OpenCartCategoryMappingItem> CategoryMappings { get; } = [];

    // Tab 4 — Ayarlar
    [ObservableProperty] private string storeUrl = "https://demo.myopencart.com";
    [ObservableProperty] private string apiKey = "oc_api_********************";
    [ObservableProperty] private string connectionTestResult = string.Empty;
    [ObservableProperty] private bool connectionTestSuccess;
    [ObservableProperty] private bool connectionTestRan;

    // Legacy compat (used by old AXAML binding)
    public ObservableCollection<PlatformOrderItem> RecentOrders { get; } = [];

    public OpenCartAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            // TODO: Replace mock with MediatR query (DEV 3 OpenCart adapter)
            LoadMockData();
            IsEmpty = Products.Count == 0 && Orders.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"OpenCart verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadMockData()
    {
        // Products
        Products.Clear();
        Products.Add(new OpenCartProductItem { Sku = "OC-001", Name = "iPhone 15 Pro 256GB", Price = 45990m, Stock = 12, InMesTech = true });
        Products.Add(new OpenCartProductItem { Sku = "OC-002", Name = "Samsung Galaxy S24", Price = 38500m, Stock = 8, InMesTech = true });
        Products.Add(new OpenCartProductItem { Sku = "OC-003", Name = "Xiaomi Redmi Note 13", Price = 12999m, Stock = 25, InMesTech = false });
        Products.Add(new OpenCartProductItem { Sku = "OC-004", Name = "Sony WH-1000XM5 Kulaklık", Price = 8750m, Stock = 5, InMesTech = true });
        Products.Add(new OpenCartProductItem { Sku = "OC-005", Name = "Apple MacBook Air M3", Price = 79900m, Stock = 3, InMesTech = false });
        Products.Add(new OpenCartProductItem { Sku = "OC-006", Name = "Logitech MX Master 3S", Price = 3299m, Stock = 18, InMesTech = true });

        ProductCount = Products.Count;

        // Orders
        Orders.Clear();
        Orders.Add(new OpenCartOrderItem { OrderNumber = "OC-20260317-001", OrderDate = "17.03.2026", CustomerName = "Ahmet Yilmaz", TotalAmount = "₺4.590,00", Status = "Onaylandi" });
        Orders.Add(new OpenCartOrderItem { OrderNumber = "OC-20260317-002", OrderDate = "17.03.2026", CustomerName = "Fatma Demir", TotalAmount = "₺12.800,00", Status = "Kargoda" });
        Orders.Add(new OpenCartOrderItem { OrderNumber = "OC-20260316-003", OrderDate = "16.03.2026", CustomerName = "Mehmet Kaya", TotalAmount = "₺38.500,00", Status = "Teslim Edildi" });
        Orders.Add(new OpenCartOrderItem { OrderNumber = "OC-20260316-004", OrderDate = "16.03.2026", CustomerName = "Elif Celik", TotalAmount = "₺1.299,00", Status = "Beklemede" });
        Orders.Add(new OpenCartOrderItem { OrderNumber = "OC-20260315-005", OrderDate = "15.03.2026", CustomerName = "Hasan Ozturk", TotalAmount = "₺7.890,00", Status = "Kargoda" });

        OrderCount = Orders.Count(o => o.Status is "Beklemede" or "Onaylandi");
        DailyRevenue = 16_690m;

        // Category mappings
        CategoryMappings.Clear();
        CategoryMappings.Add(new OpenCartCategoryMappingItem { PlatformCategory = "Electronics > Phones", MesTechCategory = "Elektronik > Telefon", IsMapped = true });
        CategoryMappings.Add(new OpenCartCategoryMappingItem { PlatformCategory = "Electronics > Computers", MesTechCategory = "Elektronik > Bilgisayar", IsMapped = true });
        CategoryMappings.Add(new OpenCartCategoryMappingItem { PlatformCategory = "Electronics > Audio", MesTechCategory = "Elektronik > Ses Sistemleri", IsMapped = true });
        CategoryMappings.Add(new OpenCartCategoryMappingItem { PlatformCategory = "Accessories > Cables", MesTechCategory = "", IsMapped = false });
        CategoryMappings.Add(new OpenCartCategoryMappingItem { PlatformCategory = "Home & Garden", MesTechCategory = "Ev & Yasam", IsMapped = true });
        CategoryMappings.Add(new OpenCartCategoryMappingItem { PlatformCategory = "Sports & Outdoors", MesTechCategory = "", IsMapped = false });

        TotalCount = Products.Count + Orders.Count;
        IsConnected = true;
        SyncStatus = "Hazir";
        LastSyncTime = DateTime.Now.AddMinutes(-15).ToString("HH:mm");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Sync()
    {
        IsLoading = true;
        try
        {
            // TODO: Replace mock with MediatR query (DEV 3 OpenCart adapter)
            SyncStatus = "Tamamlandi";
            LastSyncTime = DateTime.Now.ToString("HH:mm");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Senkronizasyon hatasi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        ConnectionTestRan = false;
        IsLoading = true;
        try
        {
            // TODO: Replace mock with MediatR query (DEV 3 OpenCart adapter)
            // Demo: always success when URL+Key not empty
            if (!string.IsNullOrWhiteSpace(StoreUrl) && !string.IsNullOrWhiteSpace(ApiKey))
            {
                ConnectionTestSuccess = true;
                ConnectionTestResult = "Baglanti basarili! OpenCart v3.0.4.0";
            }
            else
            {
                ConnectionTestSuccess = false;
                ConnectionTestResult = "URL veya API Key bos birakilamaz.";
            }
            ConnectionTestRan = true;
        }
        catch (Exception ex)
        {
            ConnectionTestSuccess = false;
            ConnectionTestResult = $"Baglanti hatasi: {ex.Message}";
            ConnectionTestRan = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class OpenCartProductItem
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool InMesTech { get; set; }
    public string InMesTechDisplay => InMesTech ? "✅" : "❌";
}

public class OpenCartOrderItem
{
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string TotalAmount { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class OpenCartCategoryMappingItem
{
    public string PlatformCategory { get; set; } = string.Empty;
    public string MesTechCategory { get; set; } = string.Empty;
    public bool IsMapped { get; set; }
    public string MappedDisplay => IsMapped ? "✅ Eslendi" : "❌ Eslemedi";
}
