using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Queries.GetOrderList;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Siparis Listesi ViewModel — DataGrid + arama + filtre.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class OrderListAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private int totalCount;

    public ObservableCollection<OrderListItemDto> Orders { get; } = [];
    private List<OrderListItemDto> _allOrders = [];

    public string[] PlatformOptions { get; } = ["Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama"];
    public string[] StatusOptions { get; } = ["Tumu", "Yeni", "Hazirlaniyor", "Kargoda", "Teslim Edildi", "Iptal", "Iade"];

    public OrderListAvaloniaViewModel(IMediator mediator)
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
            var result = await _mediator.Send(new GetOrderListQuery(Guid.Empty));

            _allOrders = result.Select(item => new OrderListItemDto
            {
                OrderNumber = item.OrderNumber,
                CustomerName = item.CustomerName ?? string.Empty,
                Platform = item.SourcePlatform ?? string.Empty,
                OrderDate = item.OrderDate.ToString("dd.MM.yyyy"),
                TotalAmount = item.TotalAmount,
                Status = item.Status,
                StatusColor = item.Status switch
                {
                    "Yeni" => "#3b82f6",
                    "Hazirlaniyor" => "#f59e0b",
                    "Kargoda" => "#8b5cf6",
                    "Teslim Edildi" => "#10b981",
                    "Iptal" => "#ef4444",
                    "Iade" => "#f97316",
                    _ => "#64748b"
                }
            }).ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Siparisler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
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

        Orders.Clear();
        foreach (var item in filtered)
            Orders.Add(item);

        TotalCount = Orders.Count;
        IsEmpty = TotalCount == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class OrderListItemDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#64748b";
}
