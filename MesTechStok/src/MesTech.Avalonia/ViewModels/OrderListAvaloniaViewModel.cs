using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Commands.ExportOrders;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Siparis Listesi ViewModel — DataGrid + arama + filtre.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class OrderListAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private int totalCount;

    // HH-DEV2-007: Date filter
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;
    [ObservableProperty] private string selectedDateRange = "Tumu";
    public string[] DateRangeOptions { get; } = ["Tumu", "Bugun", "Bu Hafta", "Bu Ay", "Ozel"];

    // HH-DEV2-008: Pagination
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private string paginationInfo = string.Empty;

    public ObservableCollection<OrderListItemDto> Orders { get; } = [];
    private List<OrderListItemDto> _allOrders = [];

    public string[] PlatformOptions { get; } = ["Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama"];
    public string[] StatusOptions { get; } = ["Tumu", "Yeni", "Hazirlaniyor", "Kargoda", "Teslim Edildi", "Iptal", "Iade"];

    public OrderListAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetOrderListQuery(_currentUser.TenantId), ct) ?? [];

            _allOrders = result.Select(item => new OrderListItemDto
            {
                OrderNumber = item.OrderNumber,
                CustomerName = item.CustomerName ?? string.Empty,
                Platform = item.SourcePlatform ?? string.Empty,
                OrderDate = item.OrderDate.ToString("dd.MM.yyyy"),
                TotalAmount = item.TotalAmount,
                Status = item.Status
            }).ToList();

            ApplyFilters();
        }, "Siparisler yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) { if (value.Length == 0 || value.Length >= 2) ApplyFilters(); }
    partial void OnSelectedPlatformChanged(string value) => ApplyFilters();
    partial void OnSelectedStatusChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allOrders.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
            filtered = filtered.Where(x =>
                x.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                x.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (SelectedPlatform != "Tumu")
            filtered = filtered.Where(x => x.Platform == SelectedPlatform);

        if (SelectedStatus != "Tumu")
            filtered = filtered.Where(x => x.Status == SelectedStatus);

        // HH-DEV2-007: Date filter
        if (StartDate.HasValue)
            filtered = filtered.Where(x => DateTime.TryParse(x.OrderDate, out var d) && d >= StartDate.Value.DateTime);
        if (EndDate.HasValue)
            filtered = filtered.Where(x => DateTime.TryParse(x.OrderDate, out var d) && d <= EndDate.Value.DateTime);

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

    // HH-DEV2-007: Quick date range setter
    partial void OnSelectedDateRangeChanged(string value)
    {
        var now = DateTime.Now;
        (StartDate, EndDate) = value switch
        {
            "Bugun" => (new DateTimeOffset(now.Date), new DateTimeOffset(now.Date.AddDays(1).AddTicks(-1))),
            "Bu Hafta" => (new DateTimeOffset(now.Date.AddDays(-(int)now.DayOfWeek + 1)), new DateTimeOffset(now)),
            "Bu Ay" => (new DateTimeOffset(new DateTime(now.Year, now.Month, 1)), new DateTimeOffset(now)),
            _ => ((DateTimeOffset?)null, (DateTimeOffset?)null)
        };
        CurrentPage = 1;
        ApplyFilters();
    }

    // HH-DEV2-008: Page commands
    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyFilters(); } }

    [RelayCommand]
    private void PrevPage() { if (CurrentPage > 1) { CurrentPage--; ApplyFilters(); } }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    // HH-DEV2-009: Orders export to Excel
    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var from = StartDate?.DateTime ?? DateTime.Now.AddDays(-30);
            var to = EndDate?.DateTime ?? DateTime.Now;
            var result = await _mediator.Send(new ExportOrdersCommand(_currentUser.TenantId, from, to), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(
                    System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Siparisler disa aktarilirken hata");
    }
}

public class OrderListItemDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
