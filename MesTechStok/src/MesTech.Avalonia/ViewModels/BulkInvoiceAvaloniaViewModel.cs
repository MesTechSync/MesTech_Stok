using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Toplu fatura olusturma ViewModel.
/// Faturalanmamis siparisleri secip toplu e-Fatura uretimi.
/// </summary>
public partial class BulkInvoiceAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    public BulkInvoiceAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    [ObservableProperty] private bool isProcessing;
    [ObservableProperty] private double progress;
    [ObservableProperty] private int selectedCount;
    [ObservableProperty] private int successCount;
    [ObservableProperty] private int failCount;
    [ObservableProperty] private bool selectAll;
    [ObservableProperty] private string selectedProvider = "Sovos";
    [ObservableProperty] private bool showResults;
    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<BulkInvoiceOrderDto> _allOrders = [];

    public ObservableCollection<BulkInvoiceOrderDto> Orders { get; } = [];
    public ObservableCollection<string> ErrorDetails { get; } = [];

    public ObservableCollection<string> Providers { get; } =
    [
        "Sovos", "GIB Portal", "Foriba", "Logo e-Fatura"
    ];

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allOrders
            : _allOrders.Where(o =>
                o.OrderId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                o.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Orders.Clear();
        foreach (var o in filtered)
            Orders.Add(o);

        UpdateSelectedCount();
        IsEmpty = Orders.Count == 0;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            ShowResults = false;
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var orderList = await _mediator.Send(
                new GetOrderListQuery(tenantId, 100), ct);

            _allOrders.Clear();
            foreach (var o in orderList)
            {
                _allOrders.Add(new()
                {
                    Id = o.Id,
                    OrderId = o.OrderNumber,
                    CustomerName = o.CustomerName ?? "-",
                    Amount = o.TotalAmount,
                    Date = o.OrderDate,
                    Platform = o.SourcePlatform ?? "-"
                });
            }

            ApplyFilter();
        }, "Toplu fatura yuklenirken hata");
    }

    partial void OnSelectAllChanged(bool value)
    {
        foreach (var order in Orders)
            order.IsSelected = value;
        UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = Orders.Count(o => o.IsSelected);
    }

    [RelayCommand]
    private async Task ProcessBulkAsync()
    {
        var selected = Orders.Where(o => o.IsSelected).ToList();
        if (selected.Count == 0) return;

        IsProcessing = true;
        ShowResults = false;
        SuccessCount = 0;
        FailCount = 0;
        Progress = 0;
        ErrorDetails.Clear();

        var provider = SelectedProvider switch
        {
            "Sovos" => InvoiceProvider.Sovos,
            "GIB Portal" => InvoiceProvider.GibPortal,
            "Foriba" => InvoiceProvider.DijitalPlanet,
            "Logo e-Fatura" => InvoiceProvider.ELogo,
            _ => InvoiceProvider.Sovos
        };

        var orderIds = selected.Select(o => o.Id).ToList();

        try
        {
            var result = await _mediator.Send(
                new BulkCreateInvoiceCommand(orderIds, provider), CancellationToken);

            SuccessCount = result.SuccessCount;
            FailCount = result.FailCount;
            Progress = 100;

            foreach (var item in result.Results.Where(r => !r.Success))
            {
                ErrorDetails.Add($"{item.OrderNumber}: {item.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ErrorDetails.Add($"Toplu fatura hatasi: {ex.Message}");
            FailCount = selected.Count;
        }

        IsProcessing = false;
        ShowResults = true;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class BulkInvoiceOrderDto : ObservableObject
{
    public Guid Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Platform { get; set; } = string.Empty;

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}
