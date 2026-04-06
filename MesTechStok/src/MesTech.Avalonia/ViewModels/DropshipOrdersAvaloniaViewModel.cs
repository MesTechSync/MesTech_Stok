using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipOrders;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dropshipping Siparisler ViewModel — siparis DataGrid + durum filtre + tedarikci iletme.
/// HH-FIX-dropship: search + sort + Excel export added.
/// </summary>
public partial class DropshipOrdersAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    // Pagination
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages = 1;
    private const int PageSize = 25;

    public ObservableCollection<DropshipOrderItemDto> Orders { get; } = [];
    public ObservableCollection<string> StatusOptions { get; } = ["Tumu", "Yeni", "Tedarikçiye İletildi", "Kargoda", "Teslim Edildi", "İptal"];

    private List<DropshipOrderItemDto> _allOrders = [];

    public DropshipOrdersAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetDropshipOrdersQuery(_currentUser.TenantId), ct) ?? [];

            _allOrders = result.Select(o => new DropshipOrderItemDto
            {
                OrderId = o.SupplierOrderRef ?? o.Id.ToString("N")[..8].ToUpper(),
                Customer = string.Empty,
                Supplier = o.DropshipSupplierId.ToString("N")[..8],
                Status = o.Status,
                CustomerPrice = 0m,
                SupplierPrice = 0m,
                NetProfit = 0m
            }).ToList();

            ApplyFilters();
        }, "Dropshipping siparisleri yuklenirken hata");
    }

    private void ApplyFilters()
    {
        var filtered = _allOrders.AsEnumerable();

        if (SelectedStatus != "Tumu")
            filtered = filtered.Where(o => o.Status == SelectedStatus);

        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(o =>
                o.OrderId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                o.Customer.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                o.Supplier.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        var list = filtered.ToList();

        list = SortColumn switch
        {
            "OrderId"       => SortAscending ? [.. list.OrderBy(o => o.OrderId)]        : [.. list.OrderByDescending(o => o.OrderId)],
            "Customer"      => SortAscending ? [.. list.OrderBy(o => o.Customer)]       : [.. list.OrderByDescending(o => o.Customer)],
            "Supplier"      => SortAscending ? [.. list.OrderBy(o => o.Supplier)]       : [.. list.OrderByDescending(o => o.Supplier)],
            "Status"        => SortAscending ? [.. list.OrderBy(o => o.Status)]         : [.. list.OrderByDescending(o => o.Status)],
            "CustomerPrice" => SortAscending ? [.. list.OrderBy(o => o.CustomerPrice)]  : [.. list.OrderByDescending(o => o.CustomerPrice)],
            "NetProfit"     => SortAscending ? [.. list.OrderBy(o => o.NetProfit)]      : [.. list.OrderByDescending(o => o.NetProfit)],
            _               => [.. list.OrderBy(o => o.OrderId)]
        };

        TotalCount = list.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        Orders.Clear();
        foreach (var item in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            Orders.Add(item);

        IsEmpty = Orders.Count == 0;
    }

    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyFilters(); } }

    [RelayCommand]
    private void PrevPage() { if (CurrentPage > 1) { CurrentPage--; ApplyFilters(); } }

    partial void OnSearchTextChanged(string value)
    {
        if (_allOrders.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedStatusChanged(string value)
    {
        if (_allOrders.Count > 0)
            ApplyFilters();
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
        ApplyFilters();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(_currentUser.TenantId, "dropship-orders", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Dropshipping siparisleri disa aktarilirken hata");
    }

    [RelayCommand]
    private Task ForwardToSupplierAsync(DropshipOrderItemDto? order)
    {
        if (order is null || order.Status != "Yeni") return Task.CompletedTask;

        order.Status = "Tedarikçiye İletildi";
        var index = Orders.IndexOf(order);
        if (index >= 0)
        {
            Orders.RemoveAt(index);
            Orders.Insert(index, order);
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
}

public class DropshipOrderItemDto
{
    public string OrderId { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CustomerPrice { get; set; }
    public decimal SupplierPrice { get; set; }
    public decimal NetProfit { get; set; }
}
