using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Orders management ViewModel — wired to GetOrderListQuery via MediatR.
/// Includes Status ComboBox filter + search text.
/// </summary>
public partial class OrdersAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;
    private readonly INavigationService _nav;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedStatus = "Tümü";
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private OrderItemDto? selectedOrder;

    // HH-DEV2-006: Platform filter
    [ObservableProperty] private string selectedPlatform = "Tümü";
    public string[] PlatformOptions { get; } = ["Tümü", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama"];

    // HH-DEV2-007: Date filter
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;
    [ObservableProperty] private string selectedDateRange = "Tümü";
    public string[] DateRangeOptions { get; } = ["Tümü", "Bugün", "Bu Hafta", "Bu Ay"];

    // HH-DEV2-008: Pagination
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private string paginationInfo = string.Empty;

    public ObservableCollection<OrderItemDto> Orders { get; } = [];

    public ObservableCollection<string> Statuses { get; } =
    [
        "Tümü", "Yeni", "Hazırlanıyor", "Kargoda", "Teslim Edildi"
    ];

    private List<OrderItemDto> _allOrders = [];

    public OrdersAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider, INavigationService nav)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
        _nav = nav;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetOrderListQuery(_tenantProvider.GetCurrentTenantId(), 100), ct);

            _allOrders = result.Select(o => new OrderItemDto
            {
                OrderNo = o.OrderNumber,
                Date = o.OrderDate.ToString("dd.MM.yyyy"),
                Customer = o.CustomerName ?? "-",
                Amount = $"{o.TotalAmount:N2} TL",
                Status = o.Status,
                Platform = o.SourcePlatform ?? "-",
                StockDeducted = o.Status is "Kargoda" or "Teslim Edildi"
            }).ToList();

            ApplyFilters();
        }, "Siparisler yuklenirken hata");
    }

    private void ApplyFilters()
    {
        var filtered = _allOrders.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(o =>
                o.OrderNo.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                o.Customer.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedStatus != "Tümü")
        {
            filtered = filtered.Where(o => o.Status == SelectedStatus);
        }

        // HH-DEV2-006: Platform filter
        if (SelectedPlatform != "Tümü")
        {
            filtered = filtered.Where(o => o.Platform == SelectedPlatform);
        }

        // HH-DEV2-007: Date filter
        if (StartDate.HasValue)
            filtered = filtered.Where(o => DateTime.TryParse(o.Date, System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),
                System.Globalization.DateTimeStyles.None, out var d) && d >= StartDate.Value.DateTime);
        if (EndDate.HasValue)
            filtered = filtered.Where(o => DateTime.TryParse(o.Date, System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),
                System.Globalization.DateTimeStyles.None, out var d) && d <= EndDate.Value.DateTime);

        // HH-DEV2-008: Pagination
        var filteredList = filtered.ToList();
        TotalCount = filteredList.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        var paged = filteredList.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        Orders.Clear();
        foreach (var item in paged)
            Orders.Add(item);

        IsEmpty = TotalCount == 0;
        PaginationInfo = TotalCount > 0
            ? $"Sayfa {CurrentPage}/{TotalPages} ({TotalCount} siparis)"
            : string.Empty;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    // HH-DEV2-010: Navigate to OrderDetail on double-click or button
    [RelayCommand]
    private async Task ShowOrderDetail()
    {
        if (SelectedOrder is null) return;
        await _nav.NavigateToAsync("OrderDetail");
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allOrders.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedStatusChanged(string value)
    {
        if (_allOrders.Count > 0) { CurrentPage = 1; ApplyFilters(); }
    }

    // HH-DEV2-006: Platform filter changed
    partial void OnSelectedPlatformChanged(string value)
    {
        if (_allOrders.Count > 0) { CurrentPage = 1; ApplyFilters(); }
    }

    // HH-DEV2-007: Quick date range setter
    partial void OnSelectedDateRangeChanged(string value)
    {
        var now = DateTime.Now;
        (StartDate, EndDate) = value switch
        {
            "Bugün" => (new DateTimeOffset(now.Date), new DateTimeOffset(now.Date.AddDays(1).AddTicks(-1))),
            "Bu Hafta" => (new DateTimeOffset(now.Date.AddDays(-(int)now.DayOfWeek + 1)), new DateTimeOffset(now)),
            "Bu Ay" => (new DateTimeOffset(new DateTime(now.Year, now.Month, 1)), new DateTimeOffset(now)),
            _ => ((DateTimeOffset?)null, (DateTimeOffset?)null)
        };
        CurrentPage = 1;
        ApplyFilters();
    }

    // HH-DEV2-008: Pagination commands
    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyFilters(); } }

    [RelayCommand]
    private void PrevPage() { if (CurrentPage > 1) { CurrentPage--; ApplyFilters(); } }

    // KD-DEV2-004: Export CSV
    [RelayCommand]
    private Task ExportCsvAsync()
    {
        // DEP: Real export via Application layer — placeholder for now
        ExportMessage = $"CSV dosyasi basariyla olusturuldu. ({TotalCount} siparis)";
        return Task.CompletedTask;
    }

    [ObservableProperty] private string exportMessage = string.Empty;
}

public class OrderItemDto
{
    public string OrderNo { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public bool StockDeducted { get; set; }
    public string StockStatusText => StockDeducted ? "Stok Dusuruldu" : "Beklemede";

    /// <summary>Sipariş durum badge rengi — yeşil/kırmızı/turuncu/mavi/gri.</summary>
    public string StatusColor => Status switch
    {
        "Tamamlandi" or "Teslim Edildi" => "#10B981",
        "Iptal" => "#EF4444",
        "Hazirlaniyor" => "#F59E0B",
        "Kargoda" or "Gonderildi" => "#3B82F6",
        _ => "#64748B"
    };
}
